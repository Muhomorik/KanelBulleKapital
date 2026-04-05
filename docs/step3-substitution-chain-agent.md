# Step 3 — Substitution Chain Agent

**Role:** Substitution and Beneficiary Analyst

Identifies what sectors, commodities, or themes benefit as capital rotates away from affected areas. Follows the full substitution chain — not just first-order effects, but the complete rotation path to who ultimately profits. Does not search the web — reasons over the Step 2 category impact map only.

---

## Pipeline Position

```mermaid
flowchart TD
    S2[Step 2 — Weekly Summary Agent\nweekly] -->|parse| DB2[(SQLite\nWeeklySummaryRuns + WeeklySummaryThemes)]
    DB2 -->|read latest| S3[Step 3 — Substitution Chain Agent\nLLM call]
    S3 -->|parse| DB3[(SQLite\nSubstitutionChainRuns + RotationChains)]
    DB3 --> S4[Step 4 — Opportunity Scan Agent]

    style S2 fill:#4a9eff,color:#fff
    style S3 fill:#4a9eff,color:#fff
    style S4 fill:#555,color:#ccc
    style DB2 fill:#e8a838,color:#fff
    style DB3 fill:#e8a838,color:#fff
```

Every step follows the same pattern: **DB → text → LLM → response → DB.** No cross-LLM calls between steps. The database is the only interface.

---

## Trigger

**Schedule:** Weekly (every Thursday 22:10 UTC). Runs automatically via the batch scheduler or can be triggered manually from the Batch tab or Weekly Summary tab at any time.

**Precondition:** A `WeeklySummaryRuns` row with Status = Success exists for the current week.

---

## Input

| Source | Table | What |
| --- | --- | --- |
| DB | `WeeklySummaryRuns` | Latest completed weekly summary (NetMood, MoodSummary) |
| DB | `WeeklySummaryThemes` | Structured themes with confidence levels (Category, Summary, Confidence, Sentiment) |

The application queries the latest `WeeklySummaryRun` with Status = Success, eager-loads `Themes` (WeeklySummaryThemes),
groups by confidence level, and formats as text for the LLM prompt. See [Example Input](#example-input) below.

---

## Data Model

Step 3 reads Step 2's output and produces its own tables. For the full Step 1 + Step 2 data model, see [Step 2 — Data Model](step2-weekly-summary-agent.md#data-model).

```mermaid
erDiagram
    WeeklySummaryRun ||--o{ WeeklySummaryTheme : "has many"
    SubstitutionChainRun ||--o{ RotationChain : "has many"
    SubstitutionChainRun }o--|| WeeklySummaryRun : "reads from"

    WeeklySummaryRun {
        guid RunId PK
        datetimeoffset WeekStart
        datetimeoffset WeekEnd
        datetimeoffset Timestamp
        string ModelId
        RunStatus Status
        string RawMarkdownOutput
        MarketSentiment NetMood
        string MoodSummary
    }

    WeeklySummaryTheme {
        guid ThemeId PK
        guid RunId FK
        string Category
        string Summary
        ConfidenceLevel Confidence "High, Moderate, Dropped"
        MarketSentiment Sentiment
    }

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
```

`WeeklySummaryRun` + `WeeklySummaryTheme` are **Step 2 output / Step 3 input.** `SubstitutionChainRun` + `RotationChain` are **Step 3 output / Step 4 input.**

---

## Step 2 — Weekly Summary Agent (upstream)

Step 2 reads 7 days of `NewsItems` rows from the DB, builds a text prompt, and asks the LLM to summarize with confidence filtering. Its output is saved to the DB — that saved output is what Step 3 reads.

See [Step 2 — Weekly Summary Agent](step2-weekly-summary-agent.md) for full details.

---

## Agent Prompt

```text
You are a substitution and beneficiary analyst. Given a weekly summary of market-moving news (aggregated from 7 daily briefs), your job is to identify what sectors, commodities, or themes benefit as capital rotates away from the affected areas.

You will receive:
1. A weekly market summary organized by news category, with confidence levels based on how consistently each theme appeared across the week.
2. The net market mood for the week.

You MUST assess every news category present in the input. Do not skip any.

For each high-confidence disruption or pressure in the input, follow the full substitution chain:
- What is being disrupted or pressured?
- What replaces it or absorbs the capital flow?
- Who profits from that rotation?
- Which fund categories capture that profit?

Then produce a summary substitution table showing: capital fleeing → flows toward → mechanism.

Rules:
- Follow the chain to its end — don't stop at the first-order effect.
- Be specific — "pipeline operators earn more via CPI-linked contracts" is better than "infrastructure benefits."
- Weight your analysis toward high-confidence themes. Ignore low-confidence one-offs.
- Do not search the web. Work only with the provided weekly summary.
- Do not invent news events — only reference themes present in the input.
- Today's date is {current_date}.

Respond ONLY with a JSON object (no markdown, no commentary). Use this exact schema:
{
  "chains": [
    {
      "capital_fleeing": "What capital is fleeing from",
      "flows_toward": "Where it flows to",
      "mechanism": "Full causal chain — follow to the end, be specific"
    }
  ]
}
```

---

## Example Input

The input is **read from the DB** — Step 2's saved weekly summary. The application queries Step 2's output table and builds this text for Step 3's prompt:

```text
WEEKLY MARKET SUMMARY — Week of March 12--18, 2026

---

HIGH CONFIDENCE (5+ of 7 days):

🔴 GEOPOLITICS / ENERGY
- Iran war / Strait of Hormuz disruption (7/7 days)
- Brent crude above $96--100 (6/7 days)
- IEA emergency reserve release provided limited relief (5/7 days)

🔴 CENTRAL BANKS
- Fed holding rates at 3.5--3.75%, hawkish signals (6/7 days)
- Rate cut expectations evaporating — probability of hold through June rose to 77% (5/7 days)
- ECB, BOE, Riksbank, SNB expected to hold (5/7 days)

🔴 MACRO / INFLATION
- US producer prices above expectations (5/7 days)
- Consumer sentiment declining, expectations sub-index down (5/7 days)
- 30-year mortgage rate jumped to 6.26% (5/7 days)

MODERATE CONFIDENCE (3--4 of 7 days):

🟢 TECH / AI
- NVIDIA GTC announcements / AI capex cycle (4/7 days)
- Amazon AWS $600B projection (3/7 days)

🔴 EQUITIES
- S&P 500 at 2026 lows, third consecutive weekly loss (4/7 days)

DROPPED (low confidence / inconsistent):
- BoJ policy direction (2x hawkish, 2x accommodative — contradictory)
- SoFi short report (1/7 days — one-off)

---

NET WEEKLY MOOD: 🔴 Risk-off (5/7 days risk-off dominant)
Stagflation risk back on the table. Rate cut hopes evaporating. Energy and inflation dominate.
```

---

## Example Output (JSON from LLM)

The agent returns JSON. The application deserializes it, saves structured data to the DB,
and renders display markdown with emojis for the WebView2 UI.

```json
{
  "chains": [
    {
      "capital_fleeing": "US growth equities",
      "flows_toward": "Real assets (gold, infrastructure)",
      "mechanism": "Energy shock → inflation fear → real asset premium rises → gold as monetary hedge + CPI-linked infrastructure contracts reprice upward. Duration compression favors real yield proxies."
    },
    {
      "capital_fleeing": "Energy-importing EM (Korea, Asia, Japan)",
      "flows_toward": "Energy-exporting EM (MENA, GCC)",
      "mechanism": "Hormuz disruption inverts terms of trade — oil exporters capture windfall, GCC sovereign wealth expands, MENA equities re-rated upward."
    },
    {
      "capital_fleeing": "Long-duration bonds",
      "flows_toward": "Short-duration / T-bills",
      "mechanism": "Fed hold at 3.5-3.75% with hawkish signals → rate cut expectations evaporate → yield curve steepens at long end → capital moves to short-duration safety."
    },
    {
      "capital_fleeing": "Consumer discretionary",
      "flows_toward": "Energy, defence, commodities",
      "mechanism": "Stagflation sector rotation — rising energy costs squeeze consumer spending, capital rotates to sectors that benefit from inflation pass-through."
    }
  ]
}
```

### Rendered Display (generated by application)

The application renders the structured data back into emoji markdown for the WebView2 UI:

```text
SUBSTITUTION CHAINS — Week of March 12--18, 2026

---

🔴 US growth equities → 🟢 Real assets (gold, infrastructure)
Energy shock → inflation fear → real asset premium rises → gold as monetary
hedge + CPI-linked infrastructure contracts reprice upward.

🔴 Energy-importing EM (Korea, Asia, Japan) → 🟢 Energy-exporting EM (MENA, GCC)
Hormuz disruption inverts terms of trade — oil exporters capture windfall,
GCC sovereign wealth expands, MENA equities re-rated upward.

🔴 Long-duration bonds → 🟢 Short-duration / T-bills
Fed hold with hawkish signals → rate cut expectations evaporate → yield curve
steepens at long end → capital moves to short-duration safety.

🔴 Consumer discretionary → 🟢 Energy, defence, commodities
Stagflation sector rotation — rising energy costs squeeze consumer spending,
capital rotates to sectors that benefit from inflation pass-through.
```

---

## Output

### LLM Response Schema

The agent returns a **JSON object** with these fields:

| Field | Required | Description |
| --- | --- | --- |
| `chains` | Yes | Array of substitution chain entries |
| `chains[].capital_fleeing` | Yes | What capital is fleeing from |
| `chains[].flows_toward` | Yes | Where capital flows to |
| `chains[].mechanism` | Yes | Full causal chain — follow to the end, be specific |

### Output JSON Schema

```mermaid
classDiagram
    class SubstitutionChainOutput {
        +List~SubstitutionChainOutputChain~ chains
    }

    class SubstitutionChainOutputChain {
        +string capital_fleeing
        +string flows_toward
        +string mechanism
    }

    SubstitutionChainOutput *-- SubstitutionChainOutputChain
```

### Processing Pipeline

```mermaid
flowchart LR
    A[Query DB\nWeeklySummaryRun + WeeklySummaryThemes] -->|text| B[LLM\nchat completions]
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
| Save run metadata | `SubstitutionChainRuns` | RunId, WeeklySummaryRunId (FK), Timestamp, ModelId, Status, Duration, InputTokens, OutputTokens, TotalTokens, **RawAgentOutput**, **RawMarkdownOutput** | One row per run. |
| Save structured data (for Step 4) | `RotationChains` | ChainId, RunId (FK), CapitalFleeing, FlowsToward, Mechanism | One row per chain entry. Parsed from the raw JSON output. |

This agent does **not** search the web. It reads Step 2's saved output from the database — no dependency on Step 1.

---

## Downstream Consumers

- **Step 4** — [Opportunity Scan Agent](step4-opportunity-scan-agent.md) (flags top rotation targets worth watching based on substitution chain signals)
- **Trend analysis** — `RotationChains` rows are timestamped via their parent `SubstitutionChainRun`. Querying the same FlowsToward target across multiple weeks detects persistent capital rotation (e.g. "capital flowing toward real assets for 3 consecutive weeks").
