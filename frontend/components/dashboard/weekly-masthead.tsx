"use client";

import { useSyncExternalStore } from "react";
import { CalendarRange } from "lucide-react";

export const WEEKLY_MASTHEAD_PANEL_ID = "weekly-masthead";

interface WeeklyMastheadProps {
  weekStart: string | null;
  weekEnd: string | null;
}

// Client-only gate — same pattern as app/page.tsx. The date range uses
// toLocaleDateString() which depends on the host locale; the dev Node process
// locale may differ from the browser's, so we defer formatting to the client
// to avoid hydration mismatches without hardcoding a locale.
const noopSubscribe = () => () => {};
const getClientMounted = () => true;
const getServerMounted = () => false;

export function WeeklyMasthead({ weekStart, weekEnd }: WeeklyMastheadProps) {
  const isMounted = useSyncExternalStore(
    noopSubscribe,
    getClientMounted,
    getServerMounted,
  );
  const range = isMounted ? formatRange(weekStart, weekEnd) : null;
  const weekNumber = isoWeekNumber(weekStart);

  return (
    <section
      id={WEEKLY_MASTHEAD_PANEL_ID}
      className="animate-fade-up stagger-1 scroll-mt-20"
      aria-label="Weekly analysis window"
    >
      <div className="rounded-md bg-primary/5 px-6 py-10 text-center ring-1 ring-primary/15">
        <p className="flex items-center justify-center gap-1.5 text-xs font-semibold uppercase tracking-[0.18em] text-primary/80">
          <CalendarRange aria-hidden="true" className="h-3.5 w-3.5" />
          The analysis below covers
        </p>

        {/* Week selector — visual affordance only, not yet wired to data */}
        <button
          type="button"
          disabled
          aria-label="Select week (historical weeks coming soon)"
          title="Historical weeks coming soon"
          className="group mt-3 flex w-full flex-wrap items-baseline justify-center gap-x-4 gap-y-2"
        >
          <span className="font-serif text-4xl font-semibold tracking-tight text-foreground sm:text-5xl">
            {range ?? (
              <span className="text-muted-foreground">— awaiting data —</span>
            )}
          </span>
          {weekNumber !== null && (
            <span className="rounded-sm bg-primary/20 px-2.5 py-1 text-xs font-bold uppercase tracking-[0.14em] text-primary">
              Week {weekNumber}
            </span>
          )}
        </button>

        <p className="mt-4 text-sm text-muted-foreground">
          Includes{" "}
          <span className="font-medium text-foreground">Weekly Themes</span>,{" "}
          <span className="font-medium text-foreground">Capital Flows</span>,
          and{" "}
          <span className="font-medium text-foreground">Opportunities</span>.
        </p>
      </div>
    </section>
  );
}

function formatRange(
  startStr: string | null,
  endStr: string | null,
): string | null {
  if (!startStr || !endStr) return null;
  const start = new Date(startStr);
  const end = new Date(endStr);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return null;

  const fmt = (d: Date, opts: Intl.DateTimeFormatOptions) =>
    d.toLocaleDateString(undefined, opts);

  const sameYear = start.getFullYear() === end.getFullYear();

  if (sameYear) {
    return `${fmt(start, { month: "short", day: "numeric" })} – ${fmt(end, {
      month: "short",
      day: "numeric",
      year: "numeric",
    })}`;
  }
  return `${fmt(start, {
    month: "short",
    day: "numeric",
    year: "numeric",
  })} – ${fmt(end, { month: "short", day: "numeric", year: "numeric" })}`;
}

// ISO 8601 week number — weeks start Monday, week 1 is the one containing the first Thursday.
function isoWeekNumber(dateStr: string | null): number | null {
  if (!dateStr) return null;
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return null;
  const utc = new Date(
    Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()),
  );
  const dayNum = utc.getUTCDay() || 7;
  utc.setUTCDate(utc.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(utc.getUTCFullYear(), 0, 1));
  return Math.ceil(((utc.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
}
