# News Brief Custom Evaluator

Source-of-truth recovery copy for the `kanelbrief-news-brief-quality` custom
evaluator in Foundry. Lives in: Foundry portal → **Build** → **Evaluations** →
**Evaluator catalog** → `kanelbrief-news-brief-quality`.

This is the **only** evaluator attached to News Brief continuous evaluation. See
[AZURE-DEPLOYMENT.md § Persistent Agent Setup](AZURE-DEPLOYMENT.md) for the rule
wiring.

| Evaluator | Status | Scope |
| --- | --- | --- |
| **Groundedness** (built-in) | ❌ Disabled — see "Why no Groundedness?" below | n/a |
| **kanelbrief-news-brief-quality** (custom, this doc) | ✅ Active | Content quality: brevity, specificity, category coverage, sentiment accuracy, language, freshness |

## Why no Groundedness?

Tried and dropped. Every run errored with:

```text
(UserError) bing_grounding tool call is currently not supported for GroundednessEvaluator evaluator.
```

`bing_grounding` isn't in Microsoft's [supported tools list for agent evaluators](https://learn.microsoft.com/azure/foundry/concepts/evaluation-evaluators/agent-evaluators#supported-tools).
As long as the agent uses Grounding with Bing Search, Groundedness can't be
re-enabled. The custom evaluator is the only quality gate.

A consequence: **fact-grounding against Bing sources isn't checked anywhere**.
Foundry redacts Bing tool outputs from the eval pipeline (see
[AZURE-DEPLOYMENT.md § Citation handling](AZURE-DEPLOYMENT.md)), so the custom
evaluator can't fact-check either.

**Debugging tip — if you suspect hallucination:** the Foundry portal **agent
playground** *does* show Bing tool output to the developer (unlike the eval
pipeline). Run the same query in the playground to see what Bing actually
returned and confirm whether claims are sourced or invented.

## Why this lives in the portal, not in code

The evaluator definition (prompt + scoring config) lives in the Foundry portal —
the prompt is plain text edited via the portal UI. This file is the **recovery
copy**: if the evaluator is accidentally deleted, paste the block below back into
the portal and you're whole again.

A previous iteration of this evaluator was source-controlled as a `.txt` file
under `FikaForecast.Application/Prompts/`. That path was abandoned when this
project moved to a portal-only workflow for evaluators (so changes round-trip
through the portal UI without redeploying code).

## Setup

1. Foundry portal → **Build** → **Evaluations** → **Evaluator catalog** → **Create**.
2. Fill in:
   - **Evaluator name**: `kanelbrief-news-brief-quality`
   - **Display name**: `KanelBrief News Brief Quality`
   - **Description**: `Content, sourcing, and sentiment checks for News Brief reports.`
   - **Evaluator type**: `Prompt based`
   - **Category**: `Quality`
   - **Scoring method**: `Ordinal` — `min_value: 1`, `max_value: 5`, `desirable_direction: increase`
3. Paste the **Evaluation prompt** below verbatim.
4. **Update** to save.
5. Attach to the continuous evaluation rule on the agent: agent → **Monitor** tab →
   gear icon → **Continuous evaluation** → **Add evaluator(s)** → pick
   `kanelbrief-news-brief-quality` → **Submit**. See
   [AZURE-DEPLOYMENT.md § Continuous Evaluation Setup](AZURE-DEPLOYMENT.md) for
   the full rule config (sample rate, role assignments).

## Evaluation prompt (paste verbatim)

> ⚠️ **Output contract — do not modify.** Foundry's prompt-based evaluator
> parser expects a JSON object with exactly two keys: `result` (integer for
> ordinal scoring) and `reason` (string). See
> [Microsoft Learn — Custom evaluators (preview) → Prompt-based evaluators](https://learn.microsoft.com/azure/foundry/concepts/evaluation-evaluators/custom-evaluators#prompt-based-evaluators).
> If the prompt drifts (e.g., asks the judge for `PASS`/`FAIL` or for a
> markdown-formatted `**Score**: 4`), Foundry can't extract a numeric value and
> the run fails with `Please provide a valid evaluator. Invalid or missing
> numeric result`. Keep the final `Output Format (JSON):` block exactly as
> written below.

```text
You are a strict quality evaluator for financial news briefs. Assess report content quality and return a single integer score.

### Input
Query: {{query}}
Response: {{response}}

---

JSON structure, schema compliance, enum values, and absence of extra content are already enforced by the structured-output pipeline — skip those checks. Fact-grounding against Bing sources cannot be checked here — Foundry redacts tool outputs from the eval pipeline. Focus on what's verifiable from the response text alone: content quality, specificity, sentiment accuracy, language, freshness.

For each requirement below, internally note PASS or FAIL with a specific example from the response, then aggregate to a final integer score per the rubric. Emit ONLY the JSON object specified at the end — no preamble, no per-rule list, no markdown.

1. **Content rules**: One sentence max per headline, no fluff/background/history, market implications only. No inline citation markers like `【6:2†source】` or `[1]` should appear in any text field — FAIL if present.
2. **Specificity**: Each headline must reference a specific named event, data point, or source (e.g., "Fed held rates at 5.25%", "Intel reported Q1 EPS"). Vague claims without an anchoring fact count as FAIL.
3. **Category coverage**: Relevant sectors from the past 48 hours are represented (at least 3-4).
4. **Sentiment accuracy (per-sector)**: Labels (RiskOn/RiskOff/Mixed) describe the SECTOR's own trajectory, NOT the overall market regime. Flag mismatches where headline/summary tone contradicts the label.
5. **Output language**: English only. Tokens from non-Latin scripts (Cyrillic, Devanagari, Arabic, CJK, etc.) are FAIL — flag specific words.
6. **Freshness**: The query contains a target date in the form "Produce the market brief for YYYY-MM-DD". Parse that date. Any dated reference in the response (explicit dates like "April 21", "Friday's close", or session-relative phrasing) should fall within 4 days of the target date (the prompt's "past 48 hours" plus up to 2 days of indexing-lag headroom). FAIL if the response sources predominantly from events older than 4 days before the target. If the response contains no dated references at all, this rule is N/A — pass.

Severity classes (apply per violation found):
- **MAJOR** = inline citation markers present (rule 1), sentiment label contradicts the sector's own headline/summary tone (rule 4), or non-Latin-script tokens present (rule 5).
- **MINOR** = any other violation: vague headline lacking a specific anchor (rule 2), category coverage below 3 sectors (rule 3), dated reference older than 4 days before the target (rule 6), one-sentence-per-headline overrun, or light fluff/background phrasing (rule 1).

Score rubric (integer 1-5, higher is better):
- **5** — zero violations.
- **4** — exactly one MINOR violation, no MAJOR.
- **3** — two or three MINOR violations, OR exactly one MAJOR.
- **2** — four+ MINOR violations, OR one MAJOR plus one+ MINOR, OR two MAJOR.
- **1** — three+ MAJOR, OR the response is unparsable, dominated by non-Latin tokens, or dominated by fluff/background prose.

Tie-breaker: when uncertain between two adjacent scores, pick the lower one. The rubric rewards genuinely clean output — do not split the difference.

Output Format (JSON only — no preamble, no markdown fences, no commentary):
{
  "result": <integer from 1 to 5>,
  "reason": "<2-3 sentences citing the specific violations that drove the score>"
}
```

## Notes on what's intentionally NOT checked

- **URLs in output** — the agent prompt forbids URLs, but enforcement of that is
  better handled by the structured-output schema (no `source` field exists).
  Re-add a rule here if the model starts leaking URLs into headlines.
- **`source` field validity** — the field doesn't exist in the schema anymore.
  See [AZURE-DEPLOYMENT.md § Citation handling](AZURE-DEPLOYMENT.md) for why
  citations come from the annotation channel instead.
- **Citation hallucination** — Groundedness handles fact-checking; the
  annotation channel handles real URLs. There's nothing for this evaluator to
  add on that front.
