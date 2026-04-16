# Plan: Sync Azure Table Storage runs → FikaForecast SQLite

## Context

Today the backend (`KanelBrief`, Azure Functions + Azure Table Storage) runs the agent pipeline daily and weekly, and the WPF desktop app (`FikaForecast`) runs the same pipeline locally against its own SQLite DB. There is no link between them. We want the desktop app to be able to pull already-computed runs down from the cloud so a user who didn't run the local pipeline can still see recent briefs, weekly summaries, substitution chains, and opportunity scans.

Goal of this change: a user opens **Settings → Sync**, enters the backend URL + a fixed bearer token, picks **Last 1 day** or **Last 1 week**, clicks **Sync now**, and the local SQLite DB is filled with any missing runs from that range. Re-clicking is safe — existing rows are skipped by Guid PK.

## Schema delta (verified across all 4 run types)

The two sides agree on identity (Guid PKs) but the WPF Domain aggregates carry three fields that the backend Azure Tables don't store:

| Field | On entity | Purpose | Handling on sync |
|---|---|---|---|
| `PromptName` | `NewsBriefRun` only | Audit (which prompt file ran) | Leave `string.Empty` — audit-only, no UI impact |
| `RawAgentOutput` | all 4 aggregates | Raw JSON agent response, audit | Leave `string.Empty` — audit-only, no UI impact |
| `RawMarkdownOutput` | all 4 aggregates | Pre-rendered markdown shown in the WebView2 browser pane | **Regenerate client-side** using the existing markdown renderers — see below |

Everything else is just name/type translation the mapper handles: `string RunId` ↔ `Guid RunId`, `CreatedAt` ↔ `Timestamp`, `DurationSeconds (double)` ↔ `Duration (TimeSpan)`, `WeeklySummaryRunId`/`SubstitutionChainRunId` string ↔ Guid, backend flat `Mood/Summary/Assessments` ↔ WPF child `NewsItem` wrapper, and child-entity Guids (`ItemId`, `AssessmentId`, `ThemeId`, `ChainId`, `TargetId`) that the backend doesn't store — the mapper generates `Guid.NewGuid()` for these on rehydrate (they're local-only FKs, safe to mint fresh).

WPF aggregates have **private setters + a static `Start()` factory** — they cannot be `JsonSerializer`-deserialized directly. Resolution: add a `public static Rehydrate(...)` factory on each of the four aggregates that constructs the state directly (bypassing `Start/Complete`).

### Regenerating `RawMarkdownOutput` on the client

The WPF codebase already has four markdown renderers in [../FikaForecast.Application/Services/](../FikaForecast.Application/Services/), each plain dependency-free classes:

- [NewsBriefMarkdownRenderer.cs](../FikaForecast.Application/Services/NewsBriefMarkdownRenderer.cs) — `Render(NewsBriefParseResult)`
- [WeeklySummaryMarkdownRenderer.cs](../FikaForecast.Application/Services/WeeklySummaryMarkdownRenderer.cs) — `Render(WeeklySummaryParseResult, DateTimeOffset weekStart, DateTimeOffset weekEnd)`
- [SubstitutionChainMarkdownRenderer.cs](../FikaForecast.Application/Services/SubstitutionChainMarkdownRenderer.cs) — `Render(SubstitutionChainParseResult, DateTimeOffset weekStart, DateTimeOffset weekEnd)`
- [OpportunityScanMarkdownRenderer.cs](../FikaForecast.Application/Services/OpportunityScanMarkdownRenderer.cs) — `Render(OpportunityScanParseResult)`

Each renderer consumes a `*ParseResult` DTO from [../FikaForecast.Application/DTOs/](../FikaForecast.Application/DTOs/). The sync mapper's job per aggregate:

1. Translate the sync DTO → the matching `*ParseResult` (near-mirror shape, mechanical).
2. Call the injected `*MarkdownRenderer.Render(parseResult, ...)`.
3. Build the domain aggregate via `Rehydrate(..., rawMarkdownOutput: renderedMarkdown, ...)`.

`SyncService` gets all four renderers injected alongside the repositories. Renderers are already registered in [../FikaForecast.Wpf/Modules/ApplicationModule.cs](../FikaForecast.Wpf/Modules/ApplicationModule.cs) — reuse that registration; no new DI wiring for them.

**Pre-existing quirk to note** (not our bug): `NewsBriefMarkdownRenderer.Render` stamps `DateTimeOffset.UtcNow` into the header, so synced runs will show today's date in the header rather than the original `CreatedAt` date. Acceptable for this iteration — if we care, add a `DateTimeOffset? header` parameter to `NewsBriefMarkdownRenderer.Render` in a follow-up.

## Backend design (`backend/`)

### Endpoints — new `SyncApi.cs`

Four GET endpoints, one per run type, in a new file [../../backend/KanelBrief.Functions/Api/SyncApi.cs](../../backend/KanelBrief.Functions/Api/SyncApi.cs):

- `GET /api/sync/news-briefs?from=YYYY-MM-DD&to=YYYY-MM-DD`
- `GET /api/sync/weekly-summaries?from=YYYY-MM-DD&to=YYYY-MM-DD`
- `GET /api/sync/substitution-chains?from=YYYY-MM-DD&to=YYYY-MM-DD`
- `GET /api/sync/opportunity-scans?from=YYYY-MM-DD&to=YYYY-MM-DD`

Kept in a new file (not added to [../../backend/KanelBrief.Functions/Api/AgentRunsApi.cs](../../backend/KanelBrief.Functions/Api/AgentRunsApi.cs)) so the bearer-token middleware opts in by function-name prefix `Sync*` and the anonymous dashboard endpoints stay unaffected. All four functions stay at `AuthorizationLevel.Anonymous` — the middleware does the gate.

Response envelope (new file [../../backend/KanelBrief.Core/Models/SyncRunsResponse.cs](../../backend/KanelBrief.Core/Models/SyncRunsResponse.cs)):

```csharp
public sealed class SyncRunsResponse<T>
{
    public string From  { get; set; } = "";
    public string To    { get; set; } = "";
    public int    Count { get; set; }
    public List<T> Runs { get; set; } = new();
}
```

Reuse existing `KanelBrief.Core.Models.*Run` as the `T` (they already serialize to camelCase via `KanelJsonOptions.CamelCase`). Validation: parse `from`/`to` with `DateOnly.TryParseExact("yyyy-MM-dd")`, require `from ≤ to`, return 400 otherwise.

### Fixed-token auth — `IFunctionsWorkerMiddleware`

New file [../../backend/KanelBrief.Functions/Middleware/BearerTokenAuthMiddleware.cs](../../backend/KanelBrief.Functions/Middleware/BearerTokenAuthMiddleware.cs):

1. If `FunctionContext.FunctionDefinition.Name` does **not** start with `"Sync"`, call `next()` — opt-in by naming convention.
2. Otherwise read the configured token from `IConfiguration["SYNC_AUTH_TOKEN"]`.
   - Empty/missing → short-circuit with **503** (fail closed, log critical).
3. Read `Authorization` header from `HttpRequestData`. Require `Bearer <token>`; mismatch → **401**.
4. On success → `await next()`.

Register in [../../backend/KanelBrief.Functions/Program.cs](../../backend/KanelBrief.Functions/Program.cs):

```csharp
.ConfigureFunctionsWebApplication(worker =>
{
    worker.UseMiddleware<BearerTokenAuthMiddleware>();
})
```

Document the short-circuit pattern (`FunctionContext.GetInvocationResult()` / `context.GetHttpResponseData()`) as a code comment — isolated-worker middleware is awkward here.

### Date-range repository query

Current [../../backend/KanelBrief.Core/Repositories/IAgentRunRepository.cs](../../backend/KanelBrief.Core/Repositories/IAgentRunRepository.cs) only exposes single-date queries. Add four range methods:

```csharp
Task<List<NewsBriefRun>>         GetNewsBriefRunsByDateRangeAsync(string fromDate, string toDate);
Task<List<WeeklySummaryRun>>     GetWeeklySummaryRunsByDateRangeAsync(string fromDate, string toDate);
Task<List<SubstitutionChainRun>> GetSubstitutionChainRunsByDateRangeAsync(string fromDate, string toDate);
Task<List<OpportunityScanRun>>   GetOpportunityScanRunsByDateRangeAsync(string fromDate, string toDate);
```

Implement in [../../backend/KanelBrief.Functions/Repositories/AgentRunRepository.cs](../../backend/KanelBrief.Functions/Repositories/AgentRunRepository.cs). `PartitionKey` is `yyyy-MM-dd` (lexicographically sortable), so one Table Storage query is enough:

```csharp
var filter = $"PartitionKey ge '{fromDate}' and PartitionKey le '{toDate}'";
var query  = _newsBriefRunsTable.QueryAsync<TableEntity>(filter);
// reuse existing internal MapToNewsBriefRun / MapToWeeklySummaryRun / ... helpers
```

Preserve existing ordering (descending `CreatedAt` for news briefs). Reuse existing `internal static` mappers — do not duplicate.

### Configuration

- Local: add `"SYNC_AUTH_TOKEN": "dev-token-change-me"` to `backend/KanelBrief.Functions/local.settings.json` under `Values` (gitignored).
- Azure: App Setting `SYNC_AUTH_TOKEN` on the Function App (32-byte random hex). Never commit.
- Document both in the backend README section for secrets.

## FikaForecast client design

### New Sync bounded context

Create folder [../FikaForecast.Application/Sync/](../FikaForecast.Application/Sync/) containing:

- `SyncRange.cs` — `enum SyncRange { OneDay, OneWeek }` + extension `ToFromDate(DateOnly todayUtc)` returning `today.AddDays(-1)` or `today.AddDays(-7)`. **Use UTC** to match backend PartitionKey convention.
- `SyncResult.cs` — immutable record with per-type counters `Fetched / Inserted / Skipped / Failed`, plus `TotalDurationMs` and `string? ErrorMessage`.
- `SyncProgressUpdate.cs` — record `(string Stage, int Done, int Total, string Message)` for `IProgress<>` binding.
- `ISyncService.cs` — `Task<SyncResult> RunAsync(SyncRange range, IProgress<SyncProgressUpdate>? progress, CancellationToken ct)`.
- `IAgentRunSyncClient.cs` — HTTP abstraction with four `FetchXxxAsync(DateOnly from, DateOnly to, CancellationToken)` methods.

#### `Dtos/` — exact wire-format mirrors

Sync-only DTOs that are **exact field-for-field mirrors of what the `/api/sync/...` endpoints return**. No extra properties, no WPF concepts. Fields match the backend wire format exactly:

- `string RunId` (not `Guid`) — matches backend `AgentRunBase.RunId`
- `string RunDate`
- `DateTimeOffset CreatedAt` (not `Timestamp`)
- `double DurationSeconds` (not `TimeSpan Duration`)
- `string ModelId`, `RunStatus Status`, `int InputTokens / OutputTokens / TotalTokens`
- Type-specific fields only as the backend stores them (e.g. `NewsBriefRun` → flat `Mood`, `Summary`, `List<SyncCategoryAssessment> Assessments` — **no** `PromptName`, **no** `RawAgentOutput`, **no** `RawMarkdownOutput`, **no** `NewsItem` wrapper).

Naming: prefix with `Sync` to make the wire origin obvious and avoid collision with the existing `FikaForecast.Application.DTOs.NewsBriefRunDto` (which is a WPF presentation projection, not a wire format):

- `SyncNewsBriefRun`, `SyncWeeklySummaryRun`, `SyncSubstitutionChainRun`, `SyncOpportunityScanRun`
- Nested: `SyncCategoryAssessment`, `SyncWeeklySummaryTheme`, `SyncRotationChain`, `SyncRotationTarget`
- Envelope: `SyncRunsResponse<T>` mirroring the backend's response wrapper (`From`, `To`, `Count`, `Runs`).

**Principle**: someone reading `SyncNewsBriefRun.cs` must be able to tell exactly what JSON comes off the wire, with no WPF-side fields to wonder about. If the backend adds a field, the sync DTO adds the same field; nothing else. **Do not** reference `KanelBrief.Core` from the WPF side.

#### `Mappers/` — translation to domain

Static `ToDomain(SyncNewsBriefRun dto) -> NewsBriefRun` per type. The mapper bridges the gap:

1. Parse `Guid.Parse(dto.RunId)`.
2. Build `TimeSpan.FromSeconds(dto.DurationSeconds)`.
3. Wrap flat assessments into a `NewsItem` (for `NewsBriefRun`) / themes / chains / targets.
4. Build a `*ParseResult`.
5. Call the injected `*MarkdownRenderer.Render(parseResult, ...)` to regenerate `RawMarkdownOutput`.
6. Call `NewsBriefRun.Rehydrate(..., rawMarkdownOutput: markdown, promptName: "", rawAgentOutput: "")`.

### Domain-layer changes — `Rehydrate` factories

Add `public static Rehydrate(...)` to each of the four aggregates. Constructs the aggregate in its persisted state (bypassing `Start/Complete`). Unknown-on-backend fields default to `string.Empty`.

Files to touch:

- [../FikaForecast.Domain/Entities/NewsBriefRun.cs](../FikaForecast.Domain/Entities/NewsBriefRun.cs)
- [../FikaForecast.Domain/Entities/WeeklySummaryRun.cs](../FikaForecast.Domain/Entities/WeeklySummaryRun.cs)
- [../FikaForecast.Domain/Entities/SubstitutionChainRun.cs](../FikaForecast.Domain/Entities/SubstitutionChainRun.cs)
- [../FikaForecast.Domain/Entities/OpportunityScanRun.cs](../FikaForecast.Domain/Entities/OpportunityScanRun.cs)
- Child value objects (`NewsItem`, `WeeklySummaryTheme`, `RotationChain`, `RotationTarget`) — confirm whether existing public constructors are sufficient or static builders need to be added.

### Idempotency — `ExistsAsync` on each repo

Add to each interface in [../FikaForecast.Application/Interfaces/](../FikaForecast.Application/Interfaces/):

```csharp
Task<bool> ExistsAsync(Guid runId, CancellationToken cancellationToken = default);
```

Implement in [../FikaForecast.Infrastructure/Persistence/](../FikaForecast.Infrastructure/Persistence/) using `AnyAsync` (no aggregate materialization, no change-tracker pollution):

```csharp
public Task<bool> ExistsAsync(Guid runId, CancellationToken ct = default)
    => _db.NewsBriefRuns.AsNoTracking().AnyAsync(r => r.RunId == runId, ct);
```

### Infrastructure: HTTP client + orchestration

New files:

- [../FikaForecast.Infrastructure/Sync/AgentRunSyncClient.cs](../FikaForecast.Infrastructure/Sync/AgentRunSyncClient.cs) — implements `IAgentRunSyncClient` via a shared `HttpClient`. Reads `SyncBaseUrl` + `SyncAuthToken` from `IUserSettingsService` on each call (so the user can change them without app restart). Builds a fresh `HttpRequestMessage` with `Authorization: Bearer <token>` per call — never mutates `DefaultRequestHeaders`. Deserializes with `JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`. Throws typed `SyncAuthException` on 401, `SyncTransportException` on 5xx / connection failures.
- [../FikaForecast.Infrastructure/Sync/SyncService.cs](../FikaForecast.Infrastructure/Sync/SyncService.cs) — implements `ISyncService`. Per-type loop:
  1. Fetch DTOs for the range.
  2. For each DTO: `if (await repo.ExistsAsync(id, ct)) skipped++; continue;`
  3. Map to domain via mapper, `repo.SaveAsync(domain, ct)`, increment `Inserted` (or `Failed` on catch — do not abort the loop).
  4. Report progress.

  **FK order**: `NewsBrief → WeeklySummary → SubstitutionChain → OpportunityScan`. Document as a code comment on `RunAsync`.

### Settings window integration

Existing pattern — [../FikaForecast.Wpf/Views/SettingsWindow.xaml](../FikaForecast.Wpf/Views/SettingsWindow.xaml) + [../FikaForecast.Wpf/ViewModels/SettingsViewModel.cs](../FikaForecast.Wpf/ViewModels/SettingsViewModel.cs) + [../FikaForecast.Wpf/Services/UserSettings.cs](../FikaForecast.Wpf/Services/UserSettings.cs) → JSON at `%LOCALAPPDATA%/FikaForecast/settings.json` — is exactly what we need.

**[UserSettings.cs](../FikaForecast.Wpf/Services/UserSettings.cs)** — add:

```csharp
public string? SyncBaseUrl   { get; set; }
public string? SyncAuthToken { get; set; }   // stored plaintext — see Risks
```

No migration: `JsonSerializer` leaves missing fields `null`.

**[SettingsViewModel.cs](../FikaForecast.Wpf/ViewModels/SettingsViewModel.cs)** — add:

- VM properties via `GetValue<T>()`/`SetValue()`: `SyncBaseUrl`, `SyncAuthToken`, `SyncRange SyncRange` (default `OneDay`), `bool IsSyncing`, `string SyncStatusText`, `int SyncProgressCurrent`, `int SyncProgressTotal`.
- Load sync fields in the ctor path alongside `LoadModelSettings`.
- **Refactor `Save()` to load-merge-save** instead of constructing a fresh `UserSettings { EnabledModelIds = ..., DefaultModelId = ... }`. The current implementation would silently wipe any other persisted fields (latent bug, surfaced by the new fields). Required for this change.
- New `AsyncCommand SyncCommand`: persist settings first, resolve `ISyncService` (injected via ctor), build an `IProgress<SyncProgressUpdate>` that marshals to the UI thread, `await _syncService.RunAsync(SyncRange, progress, CancellationToken.None)`, set `SyncStatusText` to a one-line summary. Handle `SyncAuthException → "Authentication failed (check token)"`, `SyncTransportException → "Sync server unreachable"`, generic → log + generic message.
- Inject `ISyncService` into the ctor — Autofac will autowire.

**[SettingsWindow.xaml](../FikaForecast.Wpf/Views/SettingsWindow.xaml)** — add a third `<TabItem Header="Sync">` alongside Models / Prompts. Contents:

1. **Connection card**: `TextBox` bound to `SyncBaseUrl`; `PasswordBox` for the token.
2. **Run sync card**: two radio buttons (1 day / 1 week) bound to `SyncRange` via an `EnumToBoolConverter`; `Button Command="{Binding SyncCommand}"` disabled while `IsSyncing`; `ProgressBar` bound to `SyncProgressCurrent`/`SyncProgressTotal` visible only while syncing; `TextBlock` bound to `SyncStatusText`.
3. Small caption: "Sync settings are applied immediately" to counteract the existing "changes take effect after restart" banner.

**[SettingsWindow.xaml.cs](../FikaForecast.Wpf/Views/SettingsWindow.xaml.cs)** — `PasswordBox.Password` isn't a DependencyProperty; add a tiny `PasswordChanged` handler that copies into `((SettingsViewModel)DataContext).SyncAuthToken`, plus set the initial `Password` in `Loaded` from the VM. Do **not** use a `TextBox` with a font trick (leaks to automation peers).

### Autofac registration

[../FikaForecast.Wpf/Modules/InfrastructureModule.cs](../FikaForecast.Wpf/Modules/InfrastructureModule.cs) — recommended minimal path (avoids adding `Autofac.Extensions.DependencyInjection`):

```csharp
builder.Register(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
       .SingleInstance();
builder.RegisterType<AgentRunSyncClient>().As<IAgentRunSyncClient>().SingleInstance();
builder.RegisterType<SyncService>().As<ISyncService>().InstancePerLifetimeScope();
```

`SettingsViewModel` is already registered in the presentation module — the new ctor parameter is resolved automatically.

## Critical files

### Backend

- [IAgentRunRepository.cs](../../backend/KanelBrief.Core/Repositories/IAgentRunRepository.cs) — add 4 range queries
- [AgentRunRepository.cs](../../backend/KanelBrief.Functions/Repositories/AgentRunRepository.cs) — implement 4 range queries
- [SyncApi.cs](../../backend/KanelBrief.Functions/Api/SyncApi.cs) — **new**, 4 `[Function("Sync*")]`
- [BearerTokenAuthMiddleware.cs](../../backend/KanelBrief.Functions/Middleware/BearerTokenAuthMiddleware.cs) — **new**
- [SyncRunsResponse.cs](../../backend/KanelBrief.Core/Models/SyncRunsResponse.cs) — **new**
- [Program.cs](../../backend/KanelBrief.Functions/Program.cs) — `UseMiddleware<BearerTokenAuthMiddleware>()`
- `backend/KanelBrief.Functions/local.settings.json` — `SYNC_AUTH_TOKEN`

### FikaForecast

- [NewsBriefRun.cs](../FikaForecast.Domain/Entities/NewsBriefRun.cs), [WeeklySummaryRun.cs](../FikaForecast.Domain/Entities/WeeklySummaryRun.cs), [SubstitutionChainRun.cs](../FikaForecast.Domain/Entities/SubstitutionChainRun.cs), [OpportunityScanRun.cs](../FikaForecast.Domain/Entities/OpportunityScanRun.cs) — `Rehydrate(...)`
- 4 repo interfaces in [../FikaForecast.Application/Interfaces/](../FikaForecast.Application/Interfaces/) — `ExistsAsync`
- 4 repo implementations in [../FikaForecast.Infrastructure/Persistence/](../FikaForecast.Infrastructure/Persistence/) — `ExistsAsync` via `AnyAsync`
- [../FikaForecast.Application/Sync/](../FikaForecast.Application/Sync/) — **new folder** (interfaces, DTOs, mappers)
- [AgentRunSyncClient.cs](../FikaForecast.Infrastructure/Sync/AgentRunSyncClient.cs) — **new**
- [SyncService.cs](../FikaForecast.Infrastructure/Sync/SyncService.cs) — **new**
- [UserSettings.cs](../FikaForecast.Wpf/Services/UserSettings.cs) — add fields
- [SettingsViewModel.cs](../FikaForecast.Wpf/ViewModels/SettingsViewModel.cs) — VM state, `SyncCommand`, `Save()` merge fix
- [SettingsWindow.xaml](../FikaForecast.Wpf/Views/SettingsWindow.xaml) — Sync tab
- [SettingsWindow.xaml.cs](../FikaForecast.Wpf/Views/SettingsWindow.xaml.cs) — `PasswordBox` handler
- [InfrastructureModule.cs](../FikaForecast.Wpf/Modules/InfrastructureModule.cs) — register `HttpClient` + sync services

## Verification

### Backend unit tests (NUnit, Azurite where needed)

- `AgentRunRepositoryRangeTests` — empty range, single-day, multi-day, partition boundaries, no rows.
- `BearerTokenAuthMiddlewareTests` — missing header → 401, wrong scheme → 401, right scheme wrong token → 401, correct token → `next()` called, missing config → 503, non-`Sync*` function → bypass.
- `SyncApiTests` — mock `IAgentRunRepository`, assert envelope shape + camelCase; bad query string → 400.
- Run: `dotnet test backend/KanelBrief.sln`.

### Local end-to-end

1. Start Azurite; set `SYNC_AUTH_TOKEN=dev-token-change-me` in `local.settings.json`.
2. `func start` the Functions project.
3. Trigger an agent run (or seed) so the four tables contain data for today and yesterday.
4. `curl -i http://localhost:7071/api/sync/news-briefs?from=<yesterday>&to=<today>` → expect **401**.
5. Same `curl` with `-H "Authorization: Bearer dev-token-change-me"` → expect **200** + `{ from, to, count, runs }`.
6. `curl -i http://localhost:7071/api/dashboard` → still **200** (regression: middleware didn't affect anonymous endpoints).

### WPF

- NUnit + AutoFixture + AutoMoq `SyncServiceTests`:
  - Repos called in FK order.
  - All-existing rows → `SaveAsync` never called, `Skipped == Fetched`.
  - Mixed existing/new → counters correct.
  - Single `SaveAsync` failure → `Failed++`, loop continues.
  - `SyncAuthException` surfaces in `SyncResult.ErrorMessage` without throwing out of `RunAsync`.
  - `IProgress<>` reports 4 stages.
- Manual: launch WPF → Settings → Sync tab → enter `http://localhost:7071` + `dev-token-change-me` → pick "1 day" → **Sync now**. Verify status counts. Click again → verify counts show skips instead of inserts (idempotency). Inspect `%LOCALAPPDATA%/FikaForecast/fikaforecast.db` with `sqlite3` to confirm rows. Open any synced run in the UI and confirm the WebView2 pane renders markdown (regenerated client-side).

## Risks / trade-offs

- **Plaintext token in `settings.json`** — matches user request and existing pattern. Anyone with `%LOCALAPPDATA%\FikaForecast\` access reads it. OK for single-user hobby use. Future hardening: `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`.
- **Schema mismatch** — adding `Rehydrate` factories touches the pure Domain layer. Alternative (map through existing `Start/Complete`) bends the lifecycle semantics; explicit reconstitution is cleaner DDD. `PromptName` and `RawAgentOutput` become `string.Empty` for synced rows (audit-only, no UI impact). `RawMarkdownOutput` is regenerated client-side via the existing renderers, so synced runs display correctly in the WebView2 pane.
- **`SettingsViewModel.Save()` currently replaces all settings** — pre-existing bug, must be fixed in this change or sync writes will wipe model settings.
- **No retry/backoff** — manual re-click is the retry. Future: Polly.
- **No pagination** — full JSON array per type per call. Fine for the current run cadence (a few rows per day); will need paging if runs become minute-level.
- **Middleware opt-in by `Sync*` name prefix** is a soft contract. Add a unit test asserting every function whose name starts with `Sync` requires a token, plus a header comment in `SyncApi.cs`.
- **Single shared token** — if multiple WPF installs ever use the same backend, they all share one secret. Out of scope.
- **Clock skew** — use `DateOnly.FromDateTime(DateTime.UtcNow.Date)` on the WPF side to match backend `RunDate` UTC convention; document in `SyncRange.ToFromDate`.
- **`NewsBriefMarkdownRenderer` header date** — the renderer stamps `DateTimeOffset.UtcNow` into the header rather than the original `CreatedAt`, so synced briefs show today's date in the title. Cosmetic, pre-existing. Follow-up: add a `DateTimeOffset? header` parameter to the renderer.
