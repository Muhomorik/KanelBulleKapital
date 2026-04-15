"use client";

import { useSyncExternalStore } from "react";

// Schedule times must match backend cron constants in
// backend/KanelBrief.Functions/Orchestration/DailyPipelineOrchestrator.cs
// (DAILY_BRIEF_SCHEDULE = "0 */4 * * *", WEEKLY_AGGREGATION_SCHEDULE = "0 17 * * 4")

// Convert a Thursday 17:00 UTC anchor to the user's local weekday + time.
function weeklyAnchorToLocal(): string {
  // Pick the next Thursday at 17:00 UTC relative to "now" so the displayed
  // weekday reflects any day-of-week rollover from the user's timezone.
  const d = new Date();
  const daysUntilThursday = (4 - d.getUTCDay() + 7) % 7;
  d.setUTCDate(d.getUTCDate() + daysUntilThursday);
  d.setUTCHours(17, 0, 0, 0);
  const weekday = d.toLocaleDateString([], { weekday: "short" });
  const time = d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return `${weekday} ${time}`;
}

const noopSubscribe = () => () => {};
const getSchedule = () =>
  `Brief every 4 hours · Weekly analysis ${weeklyAnchorToLocal()}`;
const getServerSchedule = () =>
  "Brief every 4 hours UTC · Weekly analysis Thu 17:00 UTC";

const BUILD_SHA = process.env.NEXT_PUBLIC_BUILD_SHA ?? "dev";
const BUILD_BRANCH = process.env.NEXT_PUBLIC_BUILD_BRANCH ?? "local";
const BUILD_TIME = process.env.NEXT_PUBLIC_BUILD_TIME ?? "";

function formatBuildTime(iso: string): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toISOString().replace("T", " ").slice(0, 16) + "Z";
}

export function Footer() {
  const schedule = useSyncExternalStore(
    noopSubscribe,
    getSchedule,
    getServerSchedule,
  );

  const buildTime = formatBuildTime(BUILD_TIME);
  const commitHref = `https://github.com/Muhomorik/KanelBulleKapital/commit/${BUILD_SHA}`;

  return (
    <footer className="mt-auto border-t border-border/60 py-6">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="flex flex-col items-center gap-2 text-xs text-muted-foreground sm:flex-row sm:justify-between">
          <p>
            <span className="font-serif font-medium text-foreground">
              SmorgasBoard
            </span>{" "}
            — AI-powered market intelligence by{" "}
            <a
              href="https://github.com/Muhomorik/KanelBulleKapital"
              target="_blank"
              rel="noopener noreferrer"
              className="underline underline-offset-2 transition-colors hover:text-foreground"
            >
              KanelBulleKapital
            </a>
          </p>
          <p className="text-center sm:text-right">{schedule}</p>
        </div>
        <div className="mt-2 text-center text-[10px] text-muted-foreground/70 sm:text-right">
          <a
            href={commitHref}
            target="_blank"
            rel="noopener noreferrer"
            className="font-mono underline-offset-2 hover:underline"
            title={`Branch: ${BUILD_BRANCH}`}
          >
            {BUILD_BRANCH}@{BUILD_SHA}
          </a>
          {buildTime && <span> · built {buildTime}</span>}
        </div>
      </div>
    </footer>
  );
}
