"use client";

import { useState } from "react";
import { AlertTriangle, X } from "lucide-react";

interface DemoBannerProps {
  /** Show the banner. False when real data is loaded successfully. */
  visible: boolean;
  /**
   * Why we're showing demo data. `error` = backend unreachable / threw,
   * `no-data` = backend responded but has nothing for this date.
   */
  reason: "error" | "no-data" | null;
}

export function DemoBanner({ visible, reason }: DemoBannerProps) {
  const [dismissed, setDismissed] = useState(false);

  if (!visible || dismissed) return null;

  const detail =
    reason === "error"
      ? "the backend is unreachable. Azure Functions cold start takes ~30s — try refreshing in a moment."
      : "no real analysis exists for this date yet.";

  return (
    <div
      role="alert"
      className="relative border-b border-amber-500/30 bg-amber-500/10 px-4 py-2.5 text-center text-sm"
    >
      <div className="mx-auto flex max-w-6xl items-center justify-center gap-2">
        <AlertTriangle className="h-3.5 w-3.5 shrink-0 text-amber-600 dark:text-amber-400" />
        <p className="text-muted-foreground">
          <span className="font-medium text-foreground">Sample data</span>
          {" — "}
          The figures below are fake; {detail}
        </p>
        <button
          onClick={() => setDismissed(true)}
          className="absolute right-3 top-1/2 -translate-y-1/2 rounded-sm p-1 text-muted-foreground transition-colors hover:text-foreground"
          aria-label="Dismiss"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  );
}
