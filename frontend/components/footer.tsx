"use client";

import { useSyncExternalStore } from "react";

// Schedule times must match backend cron constants in
// backend/KanelBrief.Functions/Orchestration/DailyPipelineOrchestrator.cs
// (DAILY_BRIEF_SCHEDULE = "0 */4 * * *", WEEKLY_AGGREGATION_SCHEDULE = "0 21 * * 4")

// Convert a Thursday 21:00 UTC anchor to the user's local weekday + time.
function weeklyAnchorToLocal(): string {
  // Pick the next Thursday at 21:00 UTC relative to "now" so the displayed
  // weekday reflects any day-of-week rollover from the user's timezone.
  const d = new Date();
  const daysUntilThursday = (4 - d.getUTCDay() + 7) % 7;
  d.setUTCDate(d.getUTCDate() + daysUntilThursday);
  d.setUTCHours(21, 0, 0, 0);
  const weekday = d.toLocaleDateString([], { weekday: "short" });
  const time = d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return `${weekday} ${time}`;
}

const noopSubscribe = () => () => {};
const getSchedule = () =>
  `Brief every 4 hours · Weekly analysis ${weeklyAnchorToLocal()}`;
const getServerSchedule = () =>
  "Brief every 4 hours UTC · Weekly analysis Thu 21:00 UTC";

export function Footer() {
  const schedule = useSyncExternalStore(
    noopSubscribe,
    getSchedule,
    getServerSchedule,
  );

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
      </div>
    </footer>
  );
}
