"use client";

import { useSyncExternalStore } from "react";

// Schedule times must match backend cron constants in
// backend/KanelBrief.Functions/Orchestration/DailyPipelineOrchestrator.cs

// Convert a UTC hour to local time string (e.g. "10:00")
function utcHourToLocal(hour: number, minute = 0): string {
  const d = new Date();
  d.setUTCHours(hour, minute, 0, 0);
  return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

const noopSubscribe = () => () => {};
const getSchedule = () =>
  `Daily brief ${utcHourToLocal(8)} · Weekly analysis Mon ${utcHourToLocal(9)}`;
const getServerSchedule = () => "Daily brief 08:00 UTC · Weekly analysis Mon 09:00 UTC";

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
