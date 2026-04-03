# FikaForecast

WPF desktop application that runs AI agents to analyze financial markets. Compare how different models handle the same analysis task, and let a judge agent evaluate report quality.

![FikaForecast Overview](docs/IMG_OVERVIEW.png)

## Tech Stack

| Layer | Technology |
| --- | --- |
| AI Platform | Azure AI Foundry (model catalog + Bing Grounding) |
| Agent Framework | Microsoft Agent Framework (Foundry Agent Service v2) |
| Desktop UI | WPF, MahApps.Metro, WebView2 |
| MVVM | DevExpress MVVM |
| DI | Autofac |
| Reactive | Rx.NET |
| Persistence | EF Core 9 + SQLite |
| Logging | NLog |

## Architecture

```text
FikaForecast/
  FikaForecast.sln
  FikaForecast.Wpf/            -- WPF startup, views, view models
  FikaForecast.Domain/         -- Core domain (no external dependencies)
  FikaForecast.Application/    -- Use cases, orchestration, interfaces
  FikaForecast.Infrastructure/ -- AI agents, EF Core, external services
```

**Domain-Driven Design** -- dependencies point inward. Domain has zero external references. Infrastructure implements application interfaces. Autofac wires everything up with per-layer modules.

## Features

### Compare models side-by-side

Run the same News Brief prompt through multiple Azure AI Foundry models in parallel. Each result shows token usage, duration, and the full markdown report rendered via WebView2.

<!-- ![Comparison View](docs/IMG_COMPARISON.png) -->

### Evaluate and rank reports

An evaluation agent checks individual reports against quality rules (formatting, source attribution, category coverage). Select multiple reports and a comparison agent ranks them with a scorecard and picks a winner.

<!-- ![Evaluation View](docs/IMG_EVALUATION.png) -->

### Browse run history

All runs are persisted to SQLite. Filter by model, inspect past reports, delete old runs. Full markdown rendering in the detail pane.

<!-- ![History View](docs/IMG_HISTORY.png) -->

### Configure models and prompts

Enable/disable models, set the default, and edit all three prompts (news brief, evaluation, comparison) directly in the app. Prompts are stored as external files with embedded-resource fallback.

<!-- ![Settings](docs/IMG_SETTINGS.png) -->

## Analysis Pipeline

Step 1 is implemented. Steps 2--6 are planned.

```mermaid
flowchart LR
    S1[Step 1\nNews Brief] --> S2[Step 2\nMacro Regime]
    S2 --> S3[Step 3\nCategory Impact]
    S1 --> S3
    S3 --> S4[Step 4\nSubstitution Chain]
    S4 --> S5[Step 5\nPortfolio Implications]
    S5 --> S6[Step 6\nOpportunity Scan]
    S6 --> OUT[Final\nMarkdown Report]

    style S1 fill:#4a9eff,color:#fff
    style S2 fill:#555,color:#999
    style S3 fill:#555,color:#999
    style S4 fill:#555,color:#999
    style S5 fill:#555,color:#999
    style S6 fill:#555,color:#999
    style OUT fill:#555,color:#999
```

| Step | Agent | What it does | Status |
| --- | --- | --- | --- |
| 1 | News Brief | Scans 14 days of news via Bing Grounding, produces categorized market brief | Done |
| 2 | Macro Regime | Classifies current regime (stagflation, risk-off, reflationary, etc.) | Planned |
| 3 | Category Impact | Maps direction + causal chains for every fund category | Planned |
| 4 | Substitution Chain | Follows disruption chains to find rotation beneficiaries | Planned |
| 5 | Portfolio Implications | Evaluates current positions against new signals | Planned |
| 6 | Opportunity Scan | Flags up to 3 uninvested categories worth watching | Planned |

## Models

All models run through **Azure AI Foundry**. Same agent, same prompt, different brain.

| Model | Role | Status |
| --- | --- | --- |
| gpt-4.1 | Flagship baseline | Deployed |
| gpt-5.4-mini | Next-gen baseline | Deployed |
| gpt-5.4 | Flagship quality benchmark | Planned |
| gpt-5.4-nano | Ultra-budget option | Planned |
| DeepSeek | Open-source heavyweight | Planned |

## Configuration

| Topic | Guide |
| --- | --- |
| AI Foundry setup, Bing Grounding, model deployments | [Azure Deployment](../docs/AZURE-DEPLOYMENT.md) |
| User secrets, Key Vault, API keys | [Secrets Management](../docs/SECRETS-MANAGEMENT.md) |

## Roadmap

- [ ] Add gpt-5.4, gpt-5.4-nano, DeepSeek model configs
- [ ] Implement pipeline steps 2--6
- [ ] Parse agent markdown output into structured domain entities (NewsItem, MarketMood)

## Documentation

- [News Brief Agent Architecture](docs/news-brief-agent-architecture.md) -- Step 1 design, Mermaid diagrams, domain model, persistence schema
