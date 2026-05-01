import { render, screen } from "@testing-library/react";
import { BriefTimestamp } from "@/components/dashboard/brief-meta";

describe("BriefTimestamp staleness disclosure", () => {
  beforeEach(() => {
    jest.useFakeTimers();
    // Wed 2026-04-29 noon UTC — between weekly cron firings (Thu 17 UTC).
    jest.setSystemTime(new Date("2026-04-29T12:00:00Z"));
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  // Bug: weekly run from Thu 2026-04-23 renders as "07:00 PM" with no date,
  // so on Wed 2026-04-29 it looks like today's analysis.
  it("includes the date when the run is from a different day than today", () => {
    render(<BriefTimestamp iso="2026-04-23T17:00:00+00:00" />);

    const el = screen.getByText(/\d/);
    expect(el.textContent).toMatch(/\b23\b/);
    // Locale-agnostic April token (en: Apr, fr: avr).
    expect(el.textContent).toMatch(/apr|avr/i);
  });

  it("shows only the time when the run is from today", () => {
    render(<BriefTimestamp iso="2026-04-29T08:00:00+00:00" />);

    const el = screen.getByText(/\d/);
    expect(el.textContent).not.toMatch(/apr|avr/i);
  });
});
