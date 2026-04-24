# Analytics & Benchmark App — Plan

Planning doc for a new standalone WPF app that consumes weekly AI-generated fund inputs, ships them to an Azure AI Foundry agent, and benchmarks the agent's recommendations over time.

> **Status:** Planning only. No code written yet. Scope may shift as we build.

## Scope

A standalone WPF analytics & benchmark app. It tracks a portfolio, ingests two weekly feeds, ships the bundle to an Azure AI Foundry agent, stores the recommendations, and scores them against what actually happened.

**Not in scope:** banking emulation, settlement engine, double-entry ledger. This app does not simulate trades — it analyzes real (or paper) holdings and measures how good the AI's calls are.

### Weekly inputs consumed

- [FUND-STATISTICS-EXPORT.md](FUND-STATISTICS-EXPORT.md) — statistics CSV + metadata companion CSV
- [weekly-report-export.md](../FikaForecast/docs/weekly-report-export.md) — three markdown reports per ISO week (`weekly-summary`, `substitution-chain`, `rotation-targets`)

Both feeds are already produced by existing tooling. This app is a downstream consumer.

## What we borrow from BankEmulatorDraft

Patterns and concepts only, re-implemented fresh in the new solution. Source: [BankEmulatorDraft](../../KanelBulleKapitalDraft/BankEmulatorDraft/BankEmulatorDraft).

| From draft | Why keep | Reshape notes |
| --- | --- | --- |
| DDD layering (Domain/Application/Infrastructure/Presentation) | Clean separation, matches project style | Same |
| Stack: .NET 9 WPF + MahApps + Autofac + DevExpress MVVM + Rx + NLog | Matches [CLAUDE.md](../CLAUDE.md) conventions | Same |
| `Fund` entity + `NavSnapshot` for history | Core primitive | Same |
| `FundHolding` with weighted-average cost | Portfolio math we need | Same |
| Strongly-typed IDs (`readonly record struct`) | Type safety | Same |
| `SimulatedTimeProvider` pattern | Useful for benchmark replay | Only on the benchmarking side; main app uses real wall-clock time |
| `InvestmentScheduler` pattern | Nice seam for "apply recommendations" | Repurposed as `RecommendationApplier`, not an order placer |

**Dropped:** double-entry ledger, `Transaction`/`JournalEntry`/`Account`, two-phase settlement, `SettlementEngine`, `TradingOrder`, EF InMemory (we need persistence).

## Feature groups

### 1. Portfolio model

- **Holdings** — per-fund position: ISIN, name, units, avg cost, cost basis. Same math as the draft (fractional units, weighted-average cost, no FIFO/LIFO).
- **NAV history** — one row per fund per day; sourced from the statistics CSV's `last_nav` column, or imported separately.
- **Cash balance** — single number, plus optional target cash reserve constraint.
- **Entry methods:**
  - Manual entry (add/edit position)
  - Import from CSV (simple shape: ISIN, units, avg cost)
  - Paper-portfolio mode (start with synthetic cash, no real positions)
- **Persistence** — SQLite via EF Core 9. Not InMemory — this app is stateful across sessions.

### 2. Weekly input ingestion

- **Import folder** setting — one folder where both feeds land.
- **File watcher** — detect new files live.
- **Bundle detector** — group files by ISO week into a weekly bundle:
  - Statistics CSV + metadata companion
  - `weekly-summary` + `substitution-chain` + `rotation-targets` markdown
- **Inputs tab** — bundles by week, completeness indicator (✓/partial/missing), raw preview.
- **Manual re-scan** button.

### 3. Portfolio snapshot export

- **Holdings CSV** — one row per fund: ISIN, name, units, avg cost, cost basis, current NAV, current value, unrealized P&L, weight %.
- **Cash summary** — available cash, total account value.
- Bundled into each analysis request automatically; also exportable manually from the Portfolio tab.

### 4. Azure AI Foundry connection

- **Microsoft Agent Framework** client wired via Autofac. Verify against current Microsoft Learn docs before building — per the freshness warning in [CLAUDE.md](../CLAUDE.md).
- **Settings** — endpoint, deployment name, agent ID, API key.
- **Secrets** — `dotnet user-secrets` locally, Azure Key Vault in production (per [SECRETS-MANAGEMENT.md](SECRETS-MANAGEMENT.md)).
- **Agent definition owned by the app** — system prompt, tool list, response schema. Version-tracked in source so prompt changes can be diffed against benchmark results.
- **Response schema** — structured JSON:
  - per-fund `Action` (Buy / Sell / Hold / Watch)
  - conviction 1-5
  - target weight %
  - rationale text
  - citations referencing specific stat columns or report lines
- **Connection test** button in settings.

### 5. Weekly analysis run

- **Trigger** — manual button or auto-run when a new complete bundle is detected.
- **Bundle assembly:**
  - Statistics CSV + metadata CSV
  - Three weekly report markdowns
  - Portfolio snapshot CSV
  - Investor profile (§9)
  - Last week's recommendations (for continuity)
- **Token budget guard** — pre-estimate, warn/cap per the budget table in [FUND-STATISTICS-EXPORT.md](FUND-STATISTICS-EXPORT.md#token-budget-reference).
- **Streaming UI** — live response, cancel button, cost readout.
- **Failure handling** — log and retry once. Never silently swallow.

### 6. Analysis storage

- **AnalysisRun** entity — ISO week, timestamp, model + prompt version, input file hashes, token counts, cost estimate, raw response, structured recommendations, status.
- **Recommendation** entity — child of run: ISIN, action, conviction, target weight, rationale, citations.
- **History tab** — browse, filter by week/fund, diff two runs side-by-side.

### 7. Benchmark

The point of the app.

- **Forward benchmark** — for each past run, watch NAVs afterwards and score:
  - Realized return if you'd followed each recommendation
  - Direction hit rate
  - vs. buy-and-hold baseline
  - vs. equal-weight baseline
  - Max drawdown
- **Historical replay** — using the borrowed `SimulatedTimeProvider` pattern and NAV history from the stats CSV, fast-forward past recs against recorded NAVs to get instant scorecards without waiting.
- **Leaderboard** — aggregate across weeks / models / prompts.
- **Prompt A/B** — rerun the same bundle through a second agent variant; compare outputs and realized performance.
- **Attribution** — which citations (stat columns vs. report narratives) correlate with better calls.

### 8. Apply recommendations (optional)

- **Advisory mode (default)** — click "Apply" per line to update holdings (adjusts units + cash, logs the action).
- **Automatic mode** — `RecommendationApplier` (repurposed `InvestmentScheduler` pattern) consumes structured recs directly.
- **Guardrails** — max order size per fund, max single-fund weight, min conviction threshold, cooldown between buys of same fund, blocklist.
- No settlement mechanics — straight state mutation with an audit row.

### 9. Investor profile

- Risk level (conservative / balanced / aggressive).
- Constraints: cash reserve floor, max single-fund weight, fund blocklist, currency preference.
- Feeds into both the agent's system prompt and the guardrails above.

### 10. UI shape

| Tab | Purpose |
| --- | --- |
| Portfolio | Holdings, cash, unrealized P&L |
| Inputs | Weekly bundles, completeness, raw preview |
| Analysis | Run control, live response, latest recs with Apply buttons |
| History | Past runs + diff |
| Benchmark | Scorecards, leaderboard, prompt A/B |
| Settings | Foundry connection, import folder, profile, auto-run, guardrails |

### 11. Cost & observability

- Token counter per run, rolling monthly total, warning threshold.
- Prompt-version log so benchmark results group by prompt.
- NLog locally; Application Insights optional in Azure.

### 12. Worth considering

- **Prior-recs feedback loop** — last week's recs feed into this week's prompt for continuity.
- **Explainability enforced in schema** — every action must cite ≥1 source (stat column or report line). Makes attribution possible.
- **What-if / dry-run** — tweak the portfolio snapshot before sending to explore scenarios.
- **Export analysis as markdown** — mirror the FikaForecast pattern (`{YYYY}-W{ww}-analysis.md` with YAML metadata + body fences) so this app's output can feed another agent downstream.
- **Offline queue** — if Foundry is unreachable, queue the bundle and retry.

## Rough build order

1. New solution, DDD skeleton, SQLite + `Fund` / `NavSnapshot` / `FundHolding`.
2. Portfolio CRUD + manual entry UI.
3. Input folder + bundle detector + Inputs tab.
4. Foundry connection + settings + one-shot run with raw response display.
5. Structured response schema + Recommendations UI + History tab.
6. Apply recommendations (advisory mode).
7. Benchmark: forward-scoring on real NAVs.
8. Benchmark: historical replay using NAV history.
9. Auto-run + guardrails + automatic apply mode.

## Open questions

- Project name? Not decided.
- Is this a new solution in this repo or a separate repo?
- MVP cutoff — which of the steps above ship in v0.1?
- Do we need a Next.js frontend mirror, or is WPF-only fine for now?
