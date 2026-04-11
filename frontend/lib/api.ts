import type {
  DashboardData,
  NewsBriefRun,
  WeeklySummaryRun,
  SubstitutionChainRun,
  OpportunityScanRun,
} from "./types";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:7071";

class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }

  get isColdStart(): boolean {
    return this.status === 502 || this.status === 503;
  }
}

async function fetchJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API_URL}${path}`);
  if (!res.ok) {
    throw new ApiError(res.status, `API error: ${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

export async function getNewsBriefs(date?: string): Promise<NewsBriefRun[]> {
  const query = date ? `?date=${date}` : "";
  return fetchJson(`/api/runs/news-briefs${query}`);
}

export async function getNewsBrief(
  runDate: string,
  runId: string,
): Promise<NewsBriefRun> {
  return fetchJson(`/api/runs/news-briefs/${runDate}/${runId}`);
}

export async function getWeeklySummaries(
  date?: string,
): Promise<WeeklySummaryRun[]> {
  const query = date ? `?date=${date}` : "";
  return fetchJson(`/api/runs/weekly-summaries${query}`);
}

export async function getWeeklySummary(
  runDate: string,
  runId: string,
): Promise<WeeklySummaryRun> {
  return fetchJson(`/api/runs/weekly-summaries/${runDate}/${runId}`);
}

export async function getSubstitutionChains(
  date?: string,
): Promise<SubstitutionChainRun[]> {
  const query = date ? `?date=${date}` : "";
  return fetchJson(`/api/runs/substitution-chains${query}`);
}

export async function getSubstitutionChain(
  runDate: string,
  runId: string,
): Promise<SubstitutionChainRun> {
  return fetchJson(`/api/runs/substitution-chains/${runDate}/${runId}`);
}

export async function getOpportunityScans(
  date?: string,
): Promise<OpportunityScanRun[]> {
  const query = date ? `?date=${date}` : "";
  return fetchJson(`/api/runs/opportunity-scans${query}`);
}

export async function getOpportunityScan(
  runDate: string,
  runId: string,
): Promise<OpportunityScanRun> {
  return fetchJson(`/api/runs/opportunity-scans/${runDate}/${runId}`);
}

export async function getDashboard(date?: string): Promise<DashboardData> {
  const query = date ? `?date=${date}` : "";
  return fetchJson<DashboardData>(`/api/dashboard${query}`);
}

export { ApiError };
