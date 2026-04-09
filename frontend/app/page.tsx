"use client";

import { useState, useEffect, useCallback } from "react";
import { Header } from "@/components/header";
import { Footer } from "@/components/footer";
import { DemoBanner } from "@/components/demo-banner";
import { ColdStartBanner } from "@/components/cold-start-banner";
import { MarketPulse } from "@/components/dashboard/market-pulse";
import { WeeklyThemes } from "@/components/dashboard/weekly-themes";
import { CapitalFlows } from "@/components/dashboard/capital-flows";
import { Opportunities } from "@/components/dashboard/opportunities";
import { getDashboard } from "@/lib/api";
import { demoDashboard } from "@/lib/demo-data";
import type { DashboardData } from "@/lib/types";
import { Separator } from "@/components/ui/separator";
import { RefreshCw, ChevronLeft, ChevronRight } from "lucide-react";

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData>(demoDashboard);
  const [isDemo, setIsDemo] = useState(true);
  const [loading, setLoading] = useState(false);
  const [coldStart, setColdStart] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [today] = useState(() => new Date().toISOString().slice(0, 10));
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [dataDate, setDataDate] = useState<string>("");

  const fetchData = useCallback(
    async (date?: string) => {
      setLoading(true);
      setError(null);
      setColdStart(false);

      const coldStartTimer = setTimeout(() => setColdStart(true), 5000);

      try {
        // If date provided, fetch that specific date; otherwise backend returns latest
        const result = await getDashboard(date);
        clearTimeout(coldStartTimer);
        setColdStart(false);

        const hasAnyData =
          result.newsBrief ||
          result.weeklySummary ||
          result.substitutionChain ||
          result.opportunityScan;

        if (hasAnyData) {
          setData(result);
          setIsDemo(false);
          const runDate =
            result.newsBrief?.runDate ??
            result.weeklySummary?.runDate ??
            result.substitutionChain?.runDate ??
            result.opportunityScan?.runDate;
          if (runDate) setDataDate(runDate);
        } else {
          setData(demoDashboard);
          setIsDemo(true);
          if (date) setDataDate(date);
        }
      } catch (err) {
        clearTimeout(coldStartTimer);
        setColdStart(false);
        setError(err instanceof Error ? err.message : "Failed to fetch data");
        setData(demoDashboard);
        setIsDemo(true);
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const navigateDate = (offset: number) => {
    const base = selectedDate ?? dataDate;
    const d = new Date(base + "T00:00:00");
    d.setDate(d.getDate() + offset);
    const newDate = d.toISOString().slice(0, 10);
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
      <DemoBanner />
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
            <time className="font-medium text-foreground">{dataDate}</time>
            {isDemo && (
              <span className="ml-2 rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                Demo data
              </span>
            )}
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
                value={selectedDate ?? dataDate}
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
              <p className="text-xs text-destructive">
                {error} — showing demo data
              </p>
            )}
          </div>
        </div>

        {/* Dashboard Grid */}
        <div className="space-y-10">
          <MarketPulse data={data.newsBrief} />
          <Separator className="opacity-50" />
          <WeeklyThemes data={data.weeklySummary} />
          <Separator className="opacity-50" />
          <CapitalFlows data={data.substitutionChain} />
          <Separator className="opacity-50" />
          <Opportunities data={data.opportunityScan} />
        </div>
      </main>

      <Footer />
    </>
  );
}
