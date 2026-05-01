import type { DashboardData } from "./types";

export const demoDashboard: DashboardData = {
  runDate: "2026-04-08",
  hasData: false,
  newsBrief: {
    runId: "demo-news-001",
    runDate: "2026-04-08",
    createdAt: "2026-04-08T08:00:00Z",
    modelId: "gpt-5.4-mini",
    status: "Success",
    durationSeconds: 11.2,
    inputTokens: 1840,
    outputTokens: 920,
    totalTokens: 2760,
    deploymentName: "gpt-5.4-mini",
    mood: "RiskOff",
    summary:
      "Markets turned cautious as tariff escalation fears gripped global trade. Energy stocks fell on demand concerns while tech showed resilience on AI infrastructure spending. Safe-haven flows boosted gold and short-duration bonds.",
    assessments: [
      {
        category: "Technology",
        headline: "AI capex shields tech from broader selloff",
        summary:
          "Hyperscaler spending plans remain intact despite macro headwinds. Semiconductor names held steady as cloud demand forecasts were reaffirmed.",
        sentiment: "RiskOn",
      },
      {
        category: "Energy",
        headline: "Crude slides on global demand downgrade",
        summary:
          "IEA cut 2026 demand forecasts citing trade friction. European natural gas softened on mild weather and strong LNG arrivals.",
        sentiment: "RiskOff",
      },
      {
        category: "Financials",
        headline: "Bank earnings mixed amid yield curve uncertainty",
        summary:
          "Net interest margins compressed for regional banks. Large-cap financials guided cautiously on loan growth outlook.",
        sentiment: "Mixed",
      },
      {
        category: "Healthcare",
        headline: "Pharma rallies on defensive rotation",
        summary:
          "Large-cap pharma outperformed as investors sought safety. Biotech M&A speculation lifted mid-caps.",
        sentiment: "RiskOn",
      },
    ],
  },
  weeklySummary: {
    runId: "demo-weekly-001",
    runDate: "2026-04-07",
    createdAt: "2026-04-07T09:00:00Z",
    modelId: "gpt-5.4-mini",
    status: "Success",
    durationSeconds: 14.8,
    inputTokens: 3200,
    outputTokens: 1100,
    totalTokens: 4300,
    periodStart: "2026-03-31T00:00:00+00:00",
    periodEnd: "2026-04-04T00:00:00+00:00",
    netMood: "Mixed",
    moodSummary:
      "Markets oscillated between trade-war anxiety and AI-led optimism, with capital rotating defensively into healthcare and gold while hyperscaler capex kept secular growth themes intact.",
    themes: [
      {
        category: "Trade War Escalation",
        summary:
          "Tariff rhetoric intensified between US and China, with new 25% duties on semiconductor equipment. Markets priced in extended supply chain disruption.",
        confidence: "High",
        sentiment: "RiskOff",
      },
      {
        category: "AI Infrastructure Boom",
        summary:
          "Hyperscaler capex guidance exceeded expectations. Data center REITs and power utilities benefited from the buildout narrative.",
        confidence: "High",
        sentiment: "RiskOn",
      },
      {
        category: "Defensive Rotation",
        summary:
          "Capital shifted from cyclicals to healthcare and utilities. Gold ETFs saw largest weekly inflows since 2024.",
        confidence: "Medium",
        sentiment: "Mixed",
      },
    ],
  },
  substitutionChain: {
    runId: "demo-chain-001",
    runDate: "2026-04-07",
    createdAt: "2026-04-07T09:15:00Z",
    modelId: "gpt-5.4-mini",
    status: "Success",
    durationSeconds: 9.5,
    inputTokens: 2100,
    outputTokens: 780,
    totalTokens: 2880,
    weeklySummaryRunId: "demo-weekly-001",
    chains: [
      {
        capitalFleeing: "Energy (Oil & Gas)",
        flowsToward: "Technology (AI Infrastructure)",
        mechanism:
          "ESG-driven reallocation combined with demand downgrade driving capital toward secular growth themes",
      },
      {
        capitalFleeing: "Consumer Discretionary",
        flowsToward: "Healthcare & Utilities",
        mechanism:
          "Risk-off sentiment pushing investors from cyclical exposure toward defensive sectors with stable cash flows",
      },
      {
        capitalFleeing: "Emerging Market Equities",
        flowsToward: "Gold & Short-Duration Bonds",
        mechanism:
          "Trade friction increasing EM risk premium; safe-haven demand accelerating on geopolitical uncertainty",
      },
    ],
  },
  opportunityScan: {
    runId: "demo-opp-001",
    runDate: "2026-04-07",
    createdAt: "2026-04-07T09:20:00Z",
    modelId: "gpt-5.4-mini",
    status: "Success",
    durationSeconds: 8.3,
    inputTokens: 1900,
    outputTokens: 720,
    totalTokens: 2620,
    substitutionChainRunId: "demo-chain-001",
    targets: [
      {
        category: "Data Center REITs",
        signalStrength: "Strong",
        rationale:
          "AI infrastructure buildout creating sustained demand for data center capacity. Power availability becoming key differentiator.",
        riskCaveat:
          "Valuations stretched at 30x FFO; rising interest rates could compress cap rates",
      },
      {
        category: "Large-Cap Pharma",
        signalStrength: "Moderate",
        rationale:
          "Defensive rotation inflows plus robust pipeline catalysts in H2 2026. Dividend yields attractive relative to compressed bond yields.",
        riskCaveat:
          "Medicare price negotiation risk; patent cliffs in 2027-2028 for key drugs",
      },
      {
        category: "Gold Miners",
        signalStrength: "Moderate",
        rationale:
          "Gold price breakout above $2,800 combined with historically low miner valuations. Operating leverage to gold price significant.",
        riskCaveat:
          "If trade tensions de-escalate, safe-haven flows could reverse rapidly",
      },
    ],
  },
};
