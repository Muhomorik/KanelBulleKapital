import { render, screen } from "@testing-library/react";
import { WeeklyMasthead } from "@/components/dashboard/weekly-masthead";

describe("WeeklyMasthead date formatting", () => {
  it("renders ISO 8601 weekStart/weekEnd from the backend without 'Invalid Date'", () => {
    render(
      <WeeklyMasthead
        weekStart="2026-04-06T00:00:00+00:00"
        weekEnd="2026-04-13T00:00:00+00:00"
      />,
    );

    expect(screen.queryByText(/Invalid Date/)).toBeNull();

    // Locale-agnostic: the range element must contain both day numbers and an
    // April month abbreviation. Different locales order day/month differently
    // (en-US "Apr 6 – Apr 13" vs en-GB "6 Apr – 13 Apr"), so we assert on
    // content rather than strict ordering.
    const range = screen.getByText(/–/);
    expect(range.textContent).toMatch(/\b6\b/);
    expect(range.textContent).toMatch(/\b13\b/);
    expect(range.textContent).toMatch(/apr|avr/i);
  });

  it("renders the ISO week number", () => {
    render(
      <WeeklyMasthead
        weekStart="2026-04-06T00:00:00+00:00"
        weekEnd="2026-04-13T00:00:00+00:00"
      />,
    );

    // Week containing Mon 2026-04-06 is ISO week 15.
    expect(screen.getByText(/Week 15/i)).toBeInTheDocument();
  });

  it("names the three sections it groups", () => {
    render(
      <WeeklyMasthead
        weekStart="2026-04-06T00:00:00+00:00"
        weekEnd="2026-04-13T00:00:00+00:00"
      />,
    );

    expect(screen.getByText("Weekly Themes")).toBeInTheDocument();
    expect(screen.getByText("Capital Flows")).toBeInTheDocument();
    expect(screen.getByText("Opportunities")).toBeInTheDocument();
  });

  it("falls back gracefully when week dates are missing", () => {
    render(<WeeklyMasthead weekStart={null} weekEnd={null} />);
    expect(screen.queryByText(/Invalid Date/)).toBeNull();
  });
});
