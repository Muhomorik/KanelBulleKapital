# CLAUDE.md

> The parent repo's [CLAUDE.md](../CLAUDE.md) covers the wider stack, Azure policy, and shared backend. This file is **FikaFinans-specific** — read both.

## What this app is

A WPF skills-portfolio piece that ports an existing Claude.ai fund-analytics workflow to **Azure AI Foundry**, running the same prompt through multiple reasoning models in parallel for side-by-side comparison. Full design rationale lives in [Docs/plan.md](Docs/plan.md) — that doc is the source of truth for any open architecture question. Current model lineup and rejected candidates: [Docs/models.md](Docs/models.md).

## Hard contracts (don't break these)

These are non-negotiable and easy to violate accidentally:

1. **Foundry-only.** Every model must be reachable via `AIProjectClient` (`Azure.AI.Projects` 2.0) + `DeclarativeAgentDefinition` from `Azure.AI.Projects.Agents`, invoked through the Responses API (`ProjectOpenAIClient.GetProjectResponsesClientForAgent`). **No second SDK path** (no Anthropic, no Bedrock, no direct OpenAI, no classic `PersistentAgentsClient`). If a model can't do Code Interpreter on Foundry, swap the model — don't write a C# fallback.
2. **Files travel via the OpenAI Files API only.** Uploaded with `purpose=Assistants` through `OpenAIFileClient` (`projectClient.ProjectOpenAIClient.GetOpenAIFileClient()`), referenced by `fileId` in `CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(fileIds: …)`. The classic `purpose=Agents` path lands files under the portal's Datasets tab as `uri_file`, which Code Interpreter can't read — that's the trap. **Never inline file contents into the prompt text** — that defeats Code Interpreter and blows up tokens.
3. **No manual RAG / no C# pre-aggregation.** Code Interpreter (Python/pandas in a sandboxed container) does all data work. No `FundDataAggregator`-style classes.
4. **Folder is the source of truth.** The sidecar at `%APPDATA%\FikaFinans\foundry-files.json` tracks `logicalName → (fileId, sourceMtime, sourceSize)`. Re-uploads are mtime-driven; never upload via the Foundry portal.
5. **Hardcoded filenames.** The canonical names live in [FikaFinans.Application/Agents/FundDataFiles.cs](FikaFinans.Application/Agents/FundDataFiles.cs): `summary.csv`, `metadata.csv`, `positions.csv`, `portfolio_structure.md`, `analytics-rotation-targets.md`, `analytics-substitution-chain.md`, `analytics-weekly-summary.md`. No fuzzy matching. Missing file → fail-fast with a clear error.

## Architecture

DDD layering. Dependencies point inward; Domain has no references.

```text
FikaFinans.Domain          ← entities (FundAnalyticsRun, ModelComparison)
FikaFinans.Application     ← ports (IFundAnalyticsAgent, IFoundryFileStore, IPromptProvider) + use cases
FikaFinans.Infrastructure  ← Foundry adapters (one Code-Interpreter agent class, parameterised by ModelId)
FikaFinans.Wpf             ← MahApps shell, "Compare Models" tab, ViewModels orchestrating parallel runs
```

### The N-model pattern

**One** agent implementation ([CodeInterpreterFundAnalyticsAgent.cs](FikaFinans.Infrastructure/Foundry/CodeInterpreterFundAnalyticsAgent.cs)) is registered once per deployed model in DI — only `ModelId` differs. Model ids live in [FoundryModelIds.cs](FikaFinans.Infrastructure/Foundry/FoundryModelIds.cs). [CompareModelsUseCase](FikaFinans.Application/UseCases/CompareModelsUseCase.cs) accepts `IEnumerable<IFundAnalyticsAgent>` and runs them via `Task.WhenAll`, so adding or removing a model is a one-line DI change. Current lineup: 2 models (`gpt-5.4-1`, `DeepSeek-R1-0528-1`) — see [Docs/models.md](Docs/models.md) for the why and the rejected candidates.

**Failure isolation:** per-model failures must not abort siblings. Each `ModelRunViewModel` owns its own status, error, and live elapsed counter — Rx streams keep the panels independent.

### File upload pipeline

Two paths through `IFoundryFileStore`, both gated by the JSON sidecar:

- **Implicit** — `EnsureFilesUploadedAsync` runs at the start of every comparison. Compares local `LastWriteTimeUtc` to cached `sourceMtime`; uploads anything `Stale` or `NotUploaded`. Silent.
- **Explicit** — `ForceReuploadAllAsync` (or single-file equivalent) deletes existing fileIds via `OpenAIFileClient.DeleteFileAsync` and re-uploads regardless of mtime. Wired to the **Refresh uploads** button. Reports progress via `IProgress<FoundryFileEntry>` so individual rows update independently.

Sidecar location: `%APPDATA%\FikaFinans\foundry-files.json`. App settings (the data folder path) live next to it at `%APPDATA%\FikaFinans\settings.json`.

### DI

Autofac, three modules in [FikaFinans.Wpf/Modules/](FikaFinans.Wpf/Modules/) plus [InfrastructureModule.cs](FikaFinans.Infrastructure/DependencyInjection/InfrastructureModule.cs):

- `ApplicationModule` — currently empty.
- `InfrastructureModule` — Azure clients, file store, three agent registrations, use case. Includes `FindMissingConfiguration` so `App.OnStartup` shows a TaskDialog instead of crashing on a missing endpoint.
- `PresentationModule` — auto-discovers `*ViewModel`, `*View`, `*Window` by name suffix.
- `NLogModule` — logger registration.

ViewModels are `InstancePerDependency` (resolved via DataContext binding); singletons are reserved for stores, clients, and the use case.

### Configuration

- **Foundry endpoint** comes from configuration key `FOUNDRY_PROJECT_ENDPOINT`. Locally: `dotnet user-secrets` on the WPF project (UserSecretsId is set in [FikaFinans.Wpf.csproj](FikaFinans.Wpf/FikaFinans.Wpf.csproj)). Production: App Service config.
- When the endpoint is missing the module registers a placeholder URI so DI builds and the app launches; any actual call surfaces through the per-panel error path.
- Auth: `DefaultAzureCredential` (Azure CLI login is enough for desktop; no managed identity needed locally).

## Build & run

All commands assume Git Bash from the `FikaFinans/` folder:

```bash
# Build / test
dotnet build FikaFinans.sln
dotnet test FikaFinans.sln                    # all test projects
dotnet test FikaFinans.Infrastructure.Tests   # one project (gated on FOUNDRY_PROJECT_ENDPOINT for integration tests)
dotnet test --filter "FullyQualifiedName~FoundryFileStoreIntegrationTests"  # one test class

# First-time secret setup (run from FikaFinans.Wpf/)
dotnet user-secrets set FOUNDRY_PROJECT_ENDPOINT "https://<your-foundry-project>" --project FikaFinans.Wpf

# Launch — primary path is F5 in Visual Studio 2022 Community.
# CLI alternative:
dotnet run --project FikaFinans.Wpf
```

`FikaFinans.Wpf` targets `net9.0-windows10.0.26100.0` (`x64` only — `Platforms=AnyCPU;x64`); the other three projects target plain `net9.0`.

## Testing conventions

- NUnit 4 + AutoFixture + AutoMoq + Moq across all four test projects.
- Always resolve SUT from AutoFixture (never `new`); AAA pattern; naming `MethodName_Scenario_ExpectedBehavior`.
- Integration tests in `FikaFinans.Infrastructure.Tests` are gated on `FOUNDRY_PROJECT_ENDPOINT` — they hit real Azure and burn tokens, so they're skipped when the env var is absent.
- Detailed patterns live in the `dotnet-unit-testing-nunit` skill.

## When in doubt

- **Microsoft Agent Framework / Foundry questions** — check Microsoft Learn via the `microsoft-learn` MCP tools first; this stack is moving fast and training data lags.
- **Picking or recommending a Foundry model** — defer to [Docs/models.md](Docs/models.md) for the full candidate workflow. Before suggesting any model, cross-check **both** the [Foundry retirement list](https://learn.microsoft.com/azure/foundry/openai/concepts/retired-models) and the model card's `Tool calling` field on [Models sold directly by Azure](https://learn.microsoft.com/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure). The capability matrix on [Tool best practices](https://learn.microsoft.com/azure/foundry/agents/concepts/tool-best-practice) has been observed to overstate support — it lists `Code Interpreter: Yes` for models whose own card says `Tool calling: No` (e.g., `DeepSeek-R1-0528`), and does not filter retired entries (e.g., `MAI-DS-R1`, retired 2026-02-27). Trust the model card and the retirement list — not the matrix.
- **WPF / MVVM patterns** — use the `dotnet-wpf-mvvm`, `wpf-fluent-design`, and `dotnet-reactive-patterns` skills. The shell is MahApps.Metro + DevExpressMvvm + Rx.NET; don't introduce a different MVVM framework.
- **Reference implementation** — the agent + DI shape mirrors [`backend/KanelBrief.Functions`](../backend/KanelBrief.Functions/) (sibling repo). Don't copy the SemanticKernel-era code from older branches; this project is Foundry-only.
