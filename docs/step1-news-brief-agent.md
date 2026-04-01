# Step 1 — News Brief Agent

**Role:** Market-Moving News Analyst

Scans the last 14 days of global news and extracts only what matters for financial markets. This is the first step in the pipeline — its output feeds into downstream agents for analysis.

---

## Agent Prompt

```text
You are a sharp financial intelligence analyst. Your job is to scan the last 14 days of global news and extract only what matters for financial markets.

Every time you run, you will:

1. Search the web for major news from the past 2 weeks across these categories:
   - Macroeconomics (inflation, GDP, employment data)
   - Central banks (Fed, ECB, BOJ, BOE decisions or signals)
   - Geopolitics (wars, sanctions, trade disputes, elections)
   - Energy & commodities (oil, gas, metals)
   - Tech & AI (major earnings, regulations, breakthroughs)
   - Corporate (major earnings surprises, bankruptcies, M&A)
   - Financial system (credit events, banking stress, currency moves)

2. For each relevant item, write one sentence max: what happened + why it matters for markets.

3. Flag the market impact direction: 🔴 Risk-off / 🟢 Risk-on / 🟡 Mixed or unclear

4. End with a 2-line overall market mood summary.

Rules:
- No fluff. No background context. No history lessons.
- If something has no clear market implication, skip it.
- Prioritize surprises and changes over expected events.
- Today's date is {current_date}.
```

---

## Example Input

The agent receives no user input beyond the current date. It autonomously searches the web for recent news.

```json
{
  "current_date": "2026-03-18"
}
```

---

## Example Output

```text
MARKET-MOVING NEWS BRIEF — March 18, 2026

---

**🔴 GEOPOLITICS / ENERGY**

- **US-Israel war on Iran** (launched ~Feb 28) is the dominant market driver: the flow of
  energy and goods through the Strait of Hormuz — a conduit for roughly a fifth of the
  world's oil and LNG — is partially disrupted, and an IEA emergency release of ~400 million
  reserve barrels provided only limited relief. Supply chain shock is feeding into production
  costs globally.
- US crude settled above $96/barrel, and Brent crude rose back above $100 on Tuesday.
  Stagflation fears are mounting.

**🔴 CENTRAL BANKS**

- The Fed concludes its March meeting today; Powell is widely expected to hold rates at
  3.5–3.75% and markets will focus on updated dot-plot projections and any hawkish language
  on inflation.
- Rate cut expectations have shifted sharply: the probability the Fed holds steady through
  June rose from 31% to 77% in just weeks, and some economists now see zero cuts in 2026.
- The RBA hiked 25bps to 4.1%; the ECB, BOE, Riksbank, and SNB are all expected to hold on
  Thursday — central banks globally are caught between an energy-driven inflation shock and
  slowing growth.
- Fed Chair Powell's term ends May 15; Trump's nominee Kevin Warsh is pending Senate
  confirmation, adding political uncertainty to the Fed outlook.

**🔴 MACRO / INFLATION**

- US producer prices in February rose more than twice as fast as expected, a bad omen ahead
  of March data that will reflect the full oil shock.
- Consumer sentiment fell to 55.5 in March, with the expectations sub-index down 4.4% as
  the war weighed on confidence.
- The 30-year mortgage rate jumped from below 6% to 6.26% in two weeks as bond markets
  priced in higher inflation.

**🔴 EQUITIES**

- The S&P 500 hit a new 2026 low on March 13, posting a third consecutive weekly loss — its
  longest losing streak in about a year.
- Markets partially recovered Mon–Tue, with the S&P 500 recovering to ~6,716 on tentative
  war-related optimism and travel/booking stocks surging on strong demand.

**🟢 TECH / AI**

- Nvidia unveiled its Vera Rubin Space-1 module at GTC 2026, targeting orbital AI
  infrastructure. Bullish signal for AI capex.
- Amazon CEO Adam Jassy reportedly told staff that AI could help AWS reach $600 billion in
  sales over 10 years, double his prior estimate.

**🔴 CORPORATE / OTHER**

- Muddy Waters Research disclosed a short position in SoFi Technologies, alleging at least
  $312 million in unrecorded debt and potential material misstatements; shares fell ~5%.

---

**OVERALL MOOD: 🔴 Risk-off, with pockets of resilience.** The Iran war has abruptly
repriced the macro outlook — stagflation risk is back on the table, rate cut hopes are
evaporating, and volatility is elevated. Tech/AI remains the lone bright spot, but energy
and inflation dominate.
```

---

## Output Schema

The agent returns structured markdown with these sections:

| Section | Required | Description |
| --------- | ---------- | ------------- |
| Title with date | Yes | `MARKET-MOVING NEWS BRIEF — {date}` |
| Category blocks | Yes | One or more of: Geopolitics/Energy, Central Banks, Macro/Inflation, Equities, Tech/AI, Corporate/Other |
| Impact flag per item | Yes | 🔴 Risk-off / 🟢 Risk-on / 🟡 Mixed |
| Overall mood summary | Yes | 2-line closing summary with dominant sentiment flag |

## Downstream Consumers

- **Step 2** — Sector Impact Mapper (takes this brief and maps it to sector-level signals)
