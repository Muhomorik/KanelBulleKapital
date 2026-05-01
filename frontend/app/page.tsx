"use client";

import { useState, useEffect, useCallback, useTransition } from "react";
import { Header } from "@/components/header";
import { Footer } from "@/components/footer";
import { DemoBanner } from "@/components/demo-banner";
import { ColdStartBanner } from "@/components/cold-start-banner";
import { MarketPulse, MARKET_PULSE_PANEL_ID } from "@/components/dashboard/market-pulse";
import { BriefSelector } from "@/components/dashboard/brief-selector";
import { WeeklyMasthead, WEEKLY_MASTHEAD_PANEL_ID } from "@/components/dashboard/weekly-masthead";
import { WeeklyThemes } from "@/components/dashboard/weekly-themes";
import { CapitalFlows, CAPITAL_FLOWS_PANEL_ID } from "@/components/dashboard/capital-flows";
import { Opportunities, OPPORTUNITIES_PANEL_ID } from "@/components/dashboard/opportunities";
import { getDashboard, getNewsBriefs } from "@/lib/api";
import { demoDashboard } from "@/lib/demo-data";
import type { DashboardData, NewsBriefRun } from "@/lib/types";
import { RefreshCw, ChevronLeft, ChevronRight } from "lucide-react";
import { useLenis } from "lenis/react";

function toLocalDateString(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

// Keyboard → section id map. Hoisted so the effect doesn't re-alloc each run.
const SCROLL_TARGETS: Record<string, string> = {
  "1": MARKET_PULSE_PANEL_ID,
  "2": WEEKLY_MASTHEAD_PANEL_ID,
  "3": CAPITAL_FLOWS_PANEL_ID,
  "4": OPPORTUNITIES_PANEL_ID,
};

export default function DashboardPage() {
  // data starts as null so each section renders a skeleton on first paint.
  // We only fall back to `demoDashboard` if the fetch fails or the backend
  // reports no real run for the selected date — and in that case we surface
  // a banner that tells the user the figures are fake.
  const [data, setData] = useState<DashboardData | null>(null);
  const [briefs, setBriefs] = useState<NewsBriefRun[]>([]);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [isDemo, setIsDemo] = useState(false);
  const [demoReason, setDemoReason] = useState<"error" | "no-data" | null>(null);
  const [loading, setLoading] = useState(true);
  const [coldStart, setColdStart] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // today = "" until after hydration — keeps server HTML in sync with client's first
  // render so derived props like `disabled` don't flip between SSR and hydration.
  const [today, setToday] = useState("");
  useEffect(() => {
    setToday(toLocalDateString(new Date()));
  }, []);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [dataDate, setDataDate] = useState("");
  const [, startSelectionTransition] = useTransition();

  // Derived during render — not via useEffect — per rerender-derived-state-no-effect.
  const selectedBrief =
    briefs.find((b) => b.runId === selectedRunId) ?? data?.newsBrief ?? null;
  // Skeletons stay up until we have *something* to render — real or fake.
  const isInitialLoad = data === null;

  const handleSelectBrief = useCallback((runId: string) => {
    startSelectionTransition(() => {
      setSelectedRunId(runId);
    });
  }, []);

  const fetchData = useCallback(
    async (date?: string) => {
      setLoading(true);
      setError(null);
      setColdStart(false);

      const coldStartTimer = setTimeout(() => setColdStart(true), 5000);

      try {
        // Parallel fetch — rule async-parallel. The dashboard endpoint gives us the
        // composite view (weekly + chains + opportunities), while the news-briefs list
        // supplies all same-day briefs for the selector.
        const [dashboardResult, briefList] = await Promise.all([
          getDashboard(date),
          getNewsBriefs(date).catch(() => [] as NewsBriefRun[]),
        ]);
        clearTimeout(coldStartTimer);
        setColdStart(false);

        if (dashboardResult.hasData) {
          setData(dashboardResult);
          setIsDemo(false);
          setDemoReason(null);
          if (dashboardResult.runDate) setDataDate(dashboardResult.runDate);
          setBriefs(briefList);
          setSelectedRunId(briefList[0]?.runId ?? dashboardResult.newsBrief?.runId ?? null);
        } else {
          setData(demoDashboard);
          setIsDemo(true);
          setDemoReason("no-data");
          setDataDate(date ?? toLocalDateString(new Date()));
          const demoList = demoDashboard.newsBrief ? [demoDashboard.newsBrief] : [];
          setBriefs(demoList);
          setSelectedRunId(demoList[0]?.runId ?? null);
        }
      } catch (err) {
        clearTimeout(coldStartTimer);
        setColdStart(false);
        setError(err instanceof Error ? err.message : "Failed to fetch data");
        setData(demoDashboard);
        setIsDemo(true);
        setDemoReason("error");
        const demoList = demoDashboard.newsBrief ? [demoDashboard.newsBrief] : [];
        setBriefs(demoList);
        setSelectedRunId(demoList[0]?.runId ?? null);
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Press 1/2/3/4 to smooth-scroll to dashboard sections — handy while recording
  // walkthrough videos. Ignored when typing in an input (e.g. the date picker).
  const lenis = useLenis();
  useEffect(() => {
    if (!lenis) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      const t = e.target as HTMLElement | null;
      if (t && (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.isContentEditable)) return;
      const id = SCROLL_TARGETS[e.key];
      if (!id) return;
      const el = document.getElementById(id);
      if (!el) return;
      e.preventDefault();
      lenis.scrollTo(el, { offset: -80, duration: 1.4 });
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [lenis]);

  const navigateDate = (offset: number) => {
    const base = selectedDate ?? dataDate;
    const d = new Date(base + "T00:00:00");
    d.setDate(d.getDate() + offset);
    const newDate = toLocalDateString(d);
    if (newDate > today) return;
    setSelectedDate(newDate);
    fetchData(newDate);
  };

  const handleDateChange = (date: string) => {
    setSelectedDate(date);
    fetchData(date);
  };

  const goToLatest = () => {
    setSelectedDate(null);
    fetchData();
  };

  return (
    <>
      <DemoBanner visible={isDemo} reason={demoReason} />
      <ColdStartBanner visible={coldStart} />
      <Header />

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6">
        {/* Hero */}
        <div className="mb-8 animate-fade-up">
          <h1 className="font-serif text-3xl font-bold tracking-tight sm:text-4xl">
            Market Intelligence
          </h1>
          <p className="mt-1.5 text-sm text-muted-foreground">
            AI-generated analysis for{" "}
            <time className="font-medium text-foreground">{dataDate || today}</time>
          </p>

          {/* Date picker & controls */}
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <div className="inline-flex items-center rounded-md border border-border bg-card">
              <button
                onClick={() => navigateDate(-1)}
                disabled={loading}
                className="inline-flex items-center justify-center rounded-l-md px-1.5 py-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:opacity-50"
                aria-label="Previous day"
              >
                <ChevronLeft className="h-3.5 w-3.5" />
              </button>
              <input
                type="date"
                aria-label="Select date"
                value={selectedDate ?? (dataDate || today)}
                max={today}
                onChange={(e) => handleDateChange(e.target.value)}
                disabled={loading}
                className="h-7 border-x border-border bg-transparent px-2 text-xs font-medium text-foreground disabled:opacity-50"
              />
              <button
                onClick={() => navigateDate(1)}
                disabled={loading || (selectedDate ?? dataDate) >= today}
                className="inline-flex items-center justify-center rounded-r-md px-1.5 py-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:opacity-50"
                aria-label="Next day"
              >
                <ChevronRight className="h-3.5 w-3.5" />
              </button>
            </div>

            {selectedDate && (
              <button
                onClick={goToLatest}
                disabled={loading}
                className="inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:opacity-50"
              >
                Latest
              </button>
            )}

            <button
              onClick={() => fetchData(selectedDate ?? undefined)}
              disabled={loading}
              className="inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground disabled:opacity-50"
            >
              <RefreshCw
                className={`h-3 w-3 ${loading ? "animate-spin" : ""}`}
              />
              {loading ? "Loading..." : "Refresh"}
            </button>

            {error && (
              <p role="alert" className="text-xs text-destructive">
                {error} — showing demo data
              </p>
            )}
          </div>
        </div>

        {/* Dashboard Grid */}
        <div className="space-y-10">
          <MarketPulse
            data={selectedBrief}
            loading={isInitialLoad}
            selector={
              <BriefSelector
                briefs={briefs}
                selectedRunId={selectedRunId}
                onSelect={handleSelectBrief}
                panelId={MARKET_PULSE_PANEL_ID}
              />
            }
          />
          <WeeklyMasthead
            periodStart={data?.weeklySummary?.periodStart ?? null}
            periodEnd={data?.weeklySummary?.periodEnd ?? null}
            loading={isInitialLoad}
          />
          <WeeklyThemes data={data?.weeklySummary ?? null} loading={isInitialLoad} />
          <CapitalFlows data={data?.substitutionChain ?? null} loading={isInitialLoad} />
          <Opportunities data={data?.opportunityScan ?? null} loading={isInitialLoad} />
        </div>
      </main>

      <Footer />
    </>
  );
}
