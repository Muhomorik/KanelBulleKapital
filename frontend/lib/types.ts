/* ═══════════════════════════════════════════════════════════════
   KanelBrief API Types — mirrors backend C# models
   ═══════════════════════════════════════════════════════════════ */

export type RunStatus = "Success" | "Partial" | "Failed";
export type MarketSentiment = "RiskOn" | "RiskOff" | "Mixed";
export type ConfidenceLevel = "High" | "Medium" | "Low";
export type SignalStrength = "Strong" | "Moderate" | "Weak";

export interface CategoryAssessment {
  category: string;
  headline: string;
  summary: string;
  sentiment: MarketSentiment;
}

export interface NewsBriefRun {
  runId: string;
  runDate: string;
  createdAt: string;
  modelId: string;
  status: RunStatus;
  durationSeconds: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  deploymentName: string;
  mood: MarketSentiment;
  summary: string;
  assessments: CategoryAssessment[];
}

export interface MarketTheme {
  theme: string;
  description: string;
  confidence: ConfidenceLevel;
}

export interface WeeklySummaryRun {
  runId: string;
  runDate: string;
  createdAt: string;
  modelId: string;
  status: RunStatus;
  durationSeconds: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  weekStart: string;
  weekEnd: string;
  netMood: MarketSentiment;
  themes: MarketTheme[];
}

export interface RotationChain {
  capitalFleeing: string;
  flowsToward: string;
  mechanism: string;
}

export interface SubstitutionChainRun {
  runId: string;
  runDate: string;
  createdAt: string;
  modelId: string;
  status: RunStatus;
  durationSeconds: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  weeklySummaryRunId: string;
  chains: RotationChain[];
}

export interface RotationTarget {
  category: string;
  signalStrength: SignalStrength;
  rationale: string;
  riskCaveat: string;
}

export interface OpportunityScanRun {
  runId: string;
  runDate: string;
  createdAt: string;
  modelId: string;
  status: RunStatus;
  durationSeconds: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  substitutionChainRunId: string;
  targets: RotationTarget[];
}

export interface DashboardData {
  newsBrief: NewsBriefRun | null;
  weeklySummary: WeeklySummaryRun | null;
  substitutionChain: SubstitutionChainRun | null;
  opportunityScan: OpportunityScanRun | null;
}
