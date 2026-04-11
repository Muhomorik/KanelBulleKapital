import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DashboardPage from "@/app/page";
import { getDashboard } from "@/lib/api";
import { demoDashboard } from "@/lib/demo-data";

// --- Mock API ---
jest.mock("@/lib/api", () => ({
  getDashboard: jest.fn(),
}));

// --- Mock child components to keep tests focused on the date picker ---
jest.mock("@/components/header", () => ({
  Header: () => <div data-testid="header" />,
}));
jest.mock("@/components/footer", () => ({
  Footer: () => <div data-testid="footer" />,
}));
jest.mock("@/components/demo-banner", () => ({
  DemoBanner: () => null,
}));
jest.mock("@/components/cold-start-banner", () => ({
  ColdStartBanner: () => null,
}));
jest.mock("@/components/dashboard/market-pulse", () => ({
  MarketPulse: () => <div data-testid="market-pulse" />,
}));
jest.mock("@/components/dashboard/weekly-themes", () => ({
  WeeklyThemes: () => <div data-testid="weekly-themes" />,
}));
jest.mock("@/components/dashboard/capital-flows", () => ({
  CapitalFlows: () => <div data-testid="capital-flows" />,
}));
jest.mock("@/components/dashboard/opportunities", () => ({
  Opportunities: () => <div data-testid="opportunities" />,
}));
jest.mock("@/components/ui/separator", () => ({
  Separator: () => <hr />,
}));

const mockGetDashboard = getDashboard as jest.MockedFunction<
  typeof getDashboard
>;

// --- Freeze "today" to 2026-04-10 ---
const FAKE_TODAY = new Date("2026-04-10T12:00:00").getTime();

beforeEach(() => {
  jest.useFakeTimers({
    now: FAKE_TODAY,
    doNotFake: ["setTimeout", "setInterval", "queueMicrotask"],
  });
  mockGetDashboard.mockResolvedValue({
    ...demoDashboard,
    hasData: true,
    runDate: "2026-04-10",
  });
});

afterEach(() => {
  jest.useRealTimers();
  jest.restoreAllMocks();
});

async function renderAndWait() {
  render(<DashboardPage />);
  await waitFor(() => {
    expect(mockGetDashboard).toHaveBeenCalled();
  });
}

describe("DashboardPage date picker", () => {
  it("shows today's date in the input immediately, before API responds", () => {
    // API never resolves — simulates slow cold start
    mockGetDashboard.mockReturnValue(new Promise(() => {}));

    render(<DashboardPage />);

    // The date input must have a valid date, never the locale placeholder
    const input = document.querySelector(
      'input[type="date"]',
    ) as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(input.value).toBe("2026-04-10");
  });

  it("shows today's date in the time element immediately, before API responds", () => {
    mockGetDashboard.mockReturnValue(new Promise(() => {}));

    render(<DashboardPage />);

    const timeEl = document.querySelector("time");
    expect(timeEl).not.toBeNull();
    expect(timeEl!.textContent).toBe("2026-04-10");
  });

  it("displays an actual date value, not a placeholder", async () => {
    await renderAndWait();

    const input = screen.getByDisplayValue("2026-04-10");
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute("type", "date");
    expect((input as HTMLInputElement).value).toBe("2026-04-10");
  });

  it("navigates to previous day when left arrow is clicked", async () => {
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
    await renderAndWait();

    const prevButton = screen.getByRole("button", { name: "Previous day" });
    await user.click(prevButton);

    await waitFor(() => {
      expect(mockGetDashboard).toHaveBeenCalledWith("2026-04-09");
    });

    expect(screen.getByDisplayValue("2026-04-09")).toBeInTheDocument();
  });

  it("navigates to next day when right arrow is clicked", async () => {
    // Start with data from a past date so next-day button is enabled
    mockGetDashboard.mockResolvedValue({
      ...demoDashboard,
      hasData: true,
      runDate: "2026-04-08",
    });
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
    await renderAndWait();

    // Go back one day first (sets selectedDate to a past date)
    const prevButton = screen.getByRole("button", { name: "Previous day" });
    await user.click(prevButton);

    await waitFor(() => {
      expect(mockGetDashboard).toHaveBeenCalledWith("2026-04-07");
    });

    // Now click next
    const nextButton = screen.getByRole("button", { name: "Next day" });
    await user.click(nextButton);

    await waitFor(() => {
      expect(mockGetDashboard).toHaveBeenCalledWith("2026-04-08");
    });
  });

  it("disables next-day button when date equals today", async () => {
    await renderAndWait();

    const nextButton = screen.getByRole("button", { name: "Next day" });
    expect(nextButton).toBeDisabled();
  });
});
