# Step 4 — Opportunity Scan Agent

**Role:** Rotation Target Scanner

Reads Step 3's substitution chains and rotation signals from the database, and flags the strongest capital rotation destinations worth watching. Distills Step 3's analysis into a ranked shortlist of actionable targets.

---

## Pipeline Position

```mermaid
flowchart TD
    S3[Step 3 — Substitution Chain Agent\nLLM call] -->|parse| DB3[(SQLite\nSubstitutionChainRuns + RotationChains)]
    DB3 -->|read latest| S4[Step 4 — Opportunity Scan Agent\nLLM call]
    S4 -->|parse| DB4[(SQLite\nOpportunityScanRuns + RotationTargets)]

    style S3 fill:#555,color:#ccc
    style S4 fill:#4a9eff,color:#fff
    style DB3 fill:#e8a838,color:#fff
    style DB4 fill:#e8a838,color:#fff
```

Every step follows the same pattern: **DB → text → LLM → response → DB.** No cross-LLM calls between steps. The database is the only interface.

---

## Trigger

**Chained:** Runs immediately after Step 3 completes successfully.

**Precondition:** A `SubstitutionChainRuns` row with Status = Success exists for the current week.

---

## Input

| Source | Table | What |
| --- | --- | --- |
| DB | `SubstitutionChainRuns` | Latest completed substitution chain analysis (RawMarkdownOutput) |
| DB | `RotationChains` | Structured rotation entries (CapitalFleeing, FlowsToward, Mechanism) |

The application queries the latest `SubstitutionChainRun` with Status = Success, eager-loads `Chains` (RotationChains),
and formats as text for the LLM prompt. See [Example Input](#example-input) below.

---

## Data Model

Step 4 reads Step 3's output and produces its own tables. For the full Step 2 + Step 3 data model, see [Step 3 — Data Model](step3-substitution-chain-agent.md#data-model).

```mermaid
erDiagram
    SubstitutionChainRun ||--o{ RotationChain : "has many"
    OpportunityScanRun ||--o{ RotationTarget : "has many"
    OpportunityScanRun }o--|| SubstitutionChainRun : "reads from"

    SubstitutionChainRun {
        guid RunId PK
        guid WeeklySummaryRunId FK
        datetimeoffset Timestamp
        string ModelId
        RunStatus Status
        timespan Duration
        int InputTokens
        int OutputTokens
        int TotalTokens
        string RawAgentOutput "raw JSON from agent"
        string RawMarkdownOutput "rendered display markdown"
    }

    RotationChain {
        guid ChainId PK
        guid RunId FK
        string CapitalFleeing
        string FlowsToward
        string Mechanism
    }

    OpportunityScanRun {
        guid RunId PK
        guid SubstitutionChainRunId FK
        datetimeoffset Timestamp
        string ModelId
        RunStatus Status
        timespan Duration
        int InputTokens
        int OutputTokens
        int TotalTokens
        string RawAgentOutput "raw JSON from agent"
        string RawMarkdownOutput "rendered display markdown"
    }

    RotationTarget {
        guid TargetId PK
        guid RunId FK
        string Category
        SignalStrength SignalStrength "Strong, Moderate"
        string Rationale
        string RiskCaveat
    }
```

`SubstitutionChainRun` + `RotationChain` are **Step 3 output / Step 4 input.** `OpportunityScanRun` + `RotationTarget` are **Step 4 output.**

---

## Step 3 — Substitution Chain Agent (upstream)

Step 3 reads Step 2's weekly summary from the DB, identifies substitution chains showing where capital rotates away from affected areas,
and saves structured rotation chain entries to the DB — that saved output is what Step 4 reads.

See [Step 3 — Substitution Chain Agent](step3-substitution-chain-agent.md) for full details.

---

## Agent Prompt

```text
You are a rotation target scanner. You will receive a substitution chain analysis showing where capital is rotating — which sectors capital is fleeing, where it is flowing, and the mechanism driving the rotation.

Your job is to identify up to 3 categories or themes that are capturing the strongest capital inflows based on the rotation signals.

For each target, provide:
- Category or theme: What is it?
- Signal strength: "strong" if multiple rotation chains converge, "moderate" if single chain.
- Rationale: One sentence connecting the rotation signal to potential opportunity.
- Risk caveat: One sentence on why this might NOT work or what could reverse the signal.

Rules:
- Only flag categories where capital is clearly flowing TO (the "flows toward" column in the substitution table).
- Rank by signal convergence — categories where multiple rotation chains converge rank higher.
- Do not search the web. Work only with the provided substitution analysis.
- Be specific — "defence ETFs benefit from stagflation sector rotation" is better than "consider defence."
- Maximum 3 targets. If nothing stands out, return an empty targets array — do not force weak signals.
- Today's date is {current_date}.

Respond ONLY with a JSON object (no markdown, no commentary). Use this exact schema:
{
  "targets": [
    {
      "category": "Category or theme name",
      "signal_strength": "strong or moderate",
      "rationale": "One sentence connecting rotation signal to opportunity",
      "risk_caveat": "One sentence on what could reverse the signal"
    }
  ]
}
```

---

## Example Input

The input is **read from the DB** — Step 3's saved substitution chain analysis:

```text
SUBSTITUTION CHAIN ANALYSIS — Week of March 12--18, 2026

Rotation chains:
- Energy supply shock → Oil/LNG exporters win → GCC sovereign wealth expands → MENA equities re-rated upward.
- Inflation fear → Real asset premium rises → Gold as monetary hedge + inflation hedge.
- Infrastructure as inflation pass-through → CPI-linked contracts reprice upward.
- AI capex immune sub-sector → hyperscaler capex decoupling from macro downturn.

Substitution table:

| Capital fleeing | Flows toward | Mechanism |
| --- | --- | --- |
| US growth equities | Real assets (gold, infra) | Duration compression, real yield proxy |
| Energy-importing EM (Korea, Asia, Japan) | Energy-exporting EM (MENA, GCC) | Terms-of-trade inversion |
| Long-duration bonds | Short-duration / T-bills | Rate hold → yield curve steepens at long end |
| Consumer discretionary | Energy, defence, commodities | Stagflation sector rotation |
```

---

## Example Output (JSON from LLM)

The agent returns JSON. The application deserializes it, saves structured data to the DB,
and renders display markdown with emojis for the WebView2 UI.

```json
{
  "targets": [
    {
      "category": "Defence / Aerospace ETFs",
      "signal_strength": "strong",
      "rationale": "Three rotation chains converge here: stagflation sector rotation away from consumer discretionary, geopolitical escalation driving defence spending, and energy-exporting sovereigns increasing military procurement.",
      "risk_caveat": "Ceasefire or de-escalation would reverse the geopolitical premium overnight."
    },
    {
      "category": "Short-duration / T-bill funds",
      "signal_strength": "strong",
      "rationale": "Rate hold at 3.5-3.75% with zero cut probability through June makes cash-like instruments attractive — capital fleeing long-duration bonds flows here.",
      "risk_caveat": "A dovish Fed surprise or sudden growth scare could trigger a duration rally, making long bonds outperform."
    },
    {
      "category": "Energy producers (upstream oil & gas)",
      "signal_strength": "moderate",
      "rationale": "Brent above $100 with Hormuz disruption is a direct tailwind for upstream producers — strong mechanism but only one chain pointing here.",
      "risk_caveat": "IEA reserve releases or demand destruction at $100+ oil could cap upside — energy is notoriously mean-reverting."
    }
  ]
}
```

### Rendered Display (generated by application)

The application renders the structured data back into emoji markdown for the WebView2 UI:

```text
ROTATION TARGETS — Week of March 12--18, 2026

---

1. 🟢 **Defence / Aerospace ETFs** — Signal: Strong
   Three rotation chains converge here: stagflation sector rotation away from consumer
   discretionary, geopolitical escalation driving defence spending, and energy-exporting
   sovereigns increasing military procurement.
   ⚠️ *Risk:* Ceasefire or de-escalation would reverse the geopolitical premium overnight.

2. 🟢 **Short-duration / T-bill funds** — Signal: Strong
   Rate hold at 3.5-3.75% with zero cut probability through June makes cash-like instruments
   attractive — capital fleeing long-duration bonds flows here.
   ⚠️ *Risk:* A dovish Fed surprise or sudden growth scare could trigger a duration rally,
   making long bonds outperform.

3. 🟡 **Energy producers (upstream oil & gas)** — Signal: Moderate
   Brent above $100 with Hormuz disruption is a direct tailwind for upstream producers —
   strong mechanism but only one chain pointing here.
   ⚠️ *Risk:* IEA reserve releases or demand destruction at $100+ oil could cap upside —
   energy is notoriously mean-reverting.
```

---

## Output

### LLM Response Schema

The agent returns a **JSON object** with these fields:

| Field | Required | Description |
| --- | --- | --- |
| `targets` | Yes | Array of rotation target entries (max 3) |
| `targets[].category` | Yes | Category or theme capturing capital inflows |
| `targets[].signal_strength` | Yes | `"strong"` (multiple chains converge) or `"moderate"` (single chain) |
| `targets[].rationale` | Yes | One sentence connecting rotation signal to opportunity |
| `targets[].risk_caveat` | Yes | One sentence on what could reverse the signal |

### Output JSON Schema

```mermaid
classDiagram
    class OpportunityScanOutput {
        +List~OpportunityScanOutputTarget~ targets
    }

    class OpportunityScanOutputTarget {
        +string category
        +string signal_strength
        +string rationale
        +string risk_caveat
    }

    OpportunityScanOutput *-- OpportunityScanOutputTarget
```

### Processing Pipeline

```mermaid
flowchart LR
    A[Query DB\nSubstitutionChainRun + RotationChains] -->|text| B[LLM\nchat completions]
    B -->|JSON| C[JsonSerializer.Deserialize]
    B -->|JSON| R[RawAgentOutput]
    C -->|structured data| D[Save to DB]
    C -->|structured data| E[Render display markdown]
    E -->|emoji markdown| F[RawMarkdownOutput]
    F --> G[WebView2 UI]
```

**Two outputs stored per run:**

- `RawAgentOutput` — the original JSON from the agent, preserved for audit
- `RawMarkdownOutput` — rendered display markdown with emojis, generated from the structured data by the application

**Parsing:** JSON deserialization is deterministic (not an LLM call).
The application renders display markdown from the structured data — consistent formatting every run.

### Persistence

| Purpose | Table | Key Columns | Notes |
| --- | --- | --- | --- |
| Save run metadata | `OpportunityScanRuns` | RunId, SubstitutionChainRunId (FK), Timestamp, ModelId, Status, Duration, InputTokens, OutputTokens, TotalTokens, **RawAgentOutput**, **RawMarkdownOutput** | One row per run. |
| Save structured data | `RotationTargets` | TargetId, RunId (FK), Category, SignalStrength, Rationale, RiskCaveat | One row per target (max 3). Parsed from the raw JSON output. |

This agent does **not** search the web. It reads Step 3's saved output from the database — no dependency on Step 1 or Step 2.

This is the final pipeline step — `RotationTargets` rows are consumed by the final report generator, not another LLM agent.

---

## Downstream Consumers

- **Final report** — All step outputs are combined into a weekly markdown report for the user.

### Trend Analysis

All pipeline runs are persisted with timestamps. Querying `RotationTargets` across multiple weeks enables trend detection:

- **Persistent targets** — "Defence ETFs flagged as Strong for 3 consecutive weeks" → high-conviction signal
- **Fading targets** — "Energy producers dropped from Strong to Moderate to absent" → rotation exhausting
- **Emerging targets** — "Short-duration funds appeared this week for the first time" → new rotation starting

This cross-run data is the foundation for future portfolio integration — matching persistent rotation trends against a fixed fund category taxonomy and using RAG to find specific buyable funds by description and name.
