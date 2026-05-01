import { render, screen } from "@testing-library/react";
import { WeeklyThemes } from "@/components/dashboard/weekly-themes";
import type { WeeklySummaryRun } from "@/lib/types";

function makeRun(overrides: Partial<WeeklySummaryRun> = {}): WeeklySummaryRun {
  return {
    runId: "run-1",
    runDate: "2026-04-13",
    createdAt: "2026-04-13T00:00:00+00:00",
    modelId: "gpt-5.4-mini",
    status: "Success",
    durationSeconds: 1,
    inputTokens: 0,
    outputTokens: 0,
    totalTokens: 0,
    periodStart: "2026-04-06T00:00:00+00:00",
    periodEnd: "2026-04-13T00:00:00+00:00",
    netMood: "Mixed",
    moodSummary: "",
    themes: [],
    ...overrides,
  };
}

describe("WeeklyThemes backend contract", () => {
  it("renders theme category and summary from the backend payload", () => {
    const run = makeRun({
      themes: [
        {
          category: "Technology Leadership",
          summary:
            "AI-driven earnings kept Technology as the clearest source of risk-on sentiment.",
          confidence: "High",
          sentiment: "RiskOn",
        },
        {
          category: "Energy and Geopolitical Risk",
          summary:
            "Oil prices were shaped by supply fears, making Energy a volatile focal point.",
          confidence: "High",
          sentiment: "Mixed",
        },
      ],
    });

    render(<WeeklyThemes data={run} />);

    expect(screen.getByText("Technology Leadership")).toBeInTheDocument();
    expect(
      screen.getByText(/AI-driven earnings kept Technology/),
    ).toBeInTheDocument();
    expect(screen.getByText("Energy and Geopolitical Risk")).toBeInTheDocument();
    expect(
      screen.getByText(/Oil prices were shaped by supply fears/),
    ).toBeInTheDocument();
  });

  it("renders moodSummary under the Weekly Mood card", () => {
    const run = makeRun({
      moodSummary:
        "Markets balanced AI-led support against rising geopolitical and inflation concerns.",
    });

    render(<WeeklyThemes data={run} />);

    expect(
      screen.getByText(/Markets balanced AI-led support/),
    ).toBeInTheDocument();
  });
});
