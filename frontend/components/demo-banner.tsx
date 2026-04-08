"use client";

import { useState } from "react";
import { FlaskConical, X } from "lucide-react";

export function DemoBanner() {
  const [dismissed, setDismissed] = useState(false);

  if (dismissed) return null;

  return (
    <div className="relative border-b border-primary/20 bg-primary/5 px-4 py-2.5 text-center text-sm">
      <div className="mx-auto flex max-w-6xl items-center justify-center gap-2">
        <FlaskConical className="h-3.5 w-3.5 shrink-0 text-primary" />
        <p className="text-muted-foreground">
          <span className="font-medium text-foreground">Demo mode</span>
          {" — "}
          This is a portfolio project showing sample data. The AI pipeline runs
          on Azure Functions with ~30s cold start.
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
