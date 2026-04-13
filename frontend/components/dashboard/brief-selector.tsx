"use client";

import { memo, useRef, useSyncExternalStore } from "react";
import type { NewsBriefRun } from "@/lib/types";

interface BriefSelectorProps {
  briefs: NewsBriefRun[];
  selectedRunId: string | null;
  onSelect: (runId: string) => void;
  /** id of the associated panel element — used for aria-controls. */
  panelId: string;
}

// Stable SSR string for a brief's hour; client-side useSyncExternalStore swaps to local time.
const noopSubscribe = () => () => {};

function formatUtcHour(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "--:--";
  return `${String(d.getUTCHours()).padStart(2, "0")}:${String(d.getUTCMinutes()).padStart(2, "0")}`;
}

function formatLocalHour(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "--:--";
  return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function BriefSelectorImpl({
  briefs,
  selectedRunId,
  onSelect,
  panelId,
}: BriefSelectorProps) {
  const tabsRef = useRef<Array<HTMLButtonElement | null>>([]);

  // Render identical strings on server + first client render to avoid hydration mismatch.
  // The subscriber never fires, but after hydration the client-snapshot is used, so we
  // derive a stable signature from the briefs list and swap to local-time labels.
  const signature = briefs.map((b) => b.createdAt).join("|");
  const labelMode = useSyncExternalStore(
    noopSubscribe,
    () => "local" as const,
    () => "utc" as const,
  );

  if (briefs.length === 0) return null;

  // Single-brief days: render as a static stamp, not an interactive tablist.
  if (briefs.length === 1) {
    const only = briefs[0];
    const label =
      labelMode === "local" ? formatLocalHour(only.createdAt) : formatUtcHour(only.createdAt);
    return (
      <div
        className="mb-4 inline-flex items-center gap-2 rounded-md border border-border/60 bg-card/60 px-2.5 py-1 text-xs text-muted-foreground"
        data-signature={signature}
      >
        <span aria-hidden className="h-1 w-1 rounded-full bg-primary/70" />
        <span className="font-mono uppercase tracking-[0.12em] tabular-nums">
          {label}
          {labelMode === "utc" && " UTC"}
        </span>
      </div>
    );
  }

  const selectedIndex = Math.max(
    0,
    briefs.findIndex((b) => b.runId === selectedRunId),
  );

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key !== "ArrowLeft" && e.key !== "ArrowRight" && e.key !== "Home" && e.key !== "End") {
      return;
    }
    e.preventDefault();
    let next = selectedIndex;
    if (e.key === "ArrowLeft") next = (selectedIndex - 1 + briefs.length) % briefs.length;
    if (e.key === "ArrowRight") next = (selectedIndex + 1) % briefs.length;
    if (e.key === "Home") next = 0;
    if (e.key === "End") next = briefs.length - 1;
    onSelect(briefs[next].runId);
    tabsRef.current[next]?.focus();
  };

  return (
    <div
      role="tablist"
      aria-label="News brief time slots"
      onKeyDown={handleKeyDown}
      className="mb-5 flex flex-wrap items-end gap-x-1 gap-y-2 border-b border-border/60"
      data-signature={signature}
    >
      {briefs.map((brief, i) => {
        const isSelected = i === selectedIndex;
        const label =
          labelMode === "local"
            ? formatLocalHour(brief.createdAt)
            : formatUtcHour(brief.createdAt);
        return (
          <button
            key={brief.runId}
            ref={(el) => {
              tabsRef.current[i] = el;
            }}
            role="tab"
            type="button"
            aria-selected={isSelected}
            aria-controls={panelId}
            tabIndex={isSelected ? 0 : -1}
            onClick={() => onSelect(brief.runId)}
            className={[
              "group relative -mb-px inline-flex items-center gap-1.5 px-2.5 py-1.5 font-mono text-[11px] uppercase tracking-[0.14em] tabular-nums transition-colors duration-150",
              "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring focus-visible:ring-offset-0",
              isSelected
                ? "text-foreground"
                : "text-muted-foreground/70 hover:text-foreground",
            ].join(" ")}
          >
            <span
              aria-hidden
              className={[
                "h-1 w-1 rounded-full transition-colors duration-150",
                isSelected ? "bg-primary" : "bg-muted-foreground/30 group-hover:bg-muted-foreground/60",
              ].join(" ")}
            />
            <span>{label}</span>
            <span
              aria-hidden
              className={[
                "pointer-events-none absolute inset-x-0 -bottom-px h-[2px] transition-transform duration-200 ease-out",
                isSelected ? "scale-x-100 bg-primary" : "scale-x-0 bg-transparent",
              ].join(" ")}
            />
          </button>
        );
      })}
      {labelMode === "utc" && (
        <span className="ml-2 pb-1.5 font-mono text-[10px] uppercase tracking-widest text-muted-foreground/50">
          UTC
        </span>
      )}
    </div>
  );
}

export const BriefSelector = memo(BriefSelectorImpl);
