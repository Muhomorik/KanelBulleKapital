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
    weekStart: "2026-04-06T00:00:00+00:00",
    weekEnd: "2026-04-13T00:00:00+00:00",
    netMood: "Mixed",
    moodSummary: "",
    themes: [],
    ...overrides,
  };
}

describe("WeeklyThemes date formatting", () => {
  it("renders ISO 8601 weekStart/weekEnd from the backend without 'Invalid Date'", () => {
    render(<WeeklyThemes data={makeRun()} />);

    expect(screen.queryByText(/Invalid Date/)).toBeNull();

    // Locale-agnostic: the range element must contain both day numbers, an
    // April month abbreviation, and an em dash. Different locales order the
    // day/month differently (en-US "Apr 6 — Apr 13" vs en-GB "6 Apr — 13 Apr"),
    // so we assert on content rather than strict ordering.
    const range = screen.getByText(/—/);
    expect(range.textContent).toMatch(/\b6\b/);
    expect(range.textContent).toMatch(/\b13\b/);
    expect(range.textContent).toMatch(/apr|avr/i);
  });
});

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
