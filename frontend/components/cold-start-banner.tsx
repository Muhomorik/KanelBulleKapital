"use client";

import { Snowflake } from "lucide-react";

interface ColdStartBannerProps {
  visible: boolean;
}

export function ColdStartBanner({ visible }: ColdStartBannerProps) {
  if (!visible) return null;

  return (
    <div
      role="status"
      className="animate-fade-in border-b border-blue-500/20 bg-blue-500/5 px-4 py-2.5 text-center text-sm"
    >
      <div className="mx-auto flex max-w-6xl items-center justify-center gap-2">
        <Snowflake className="h-3.5 w-3.5 shrink-0 animate-spin text-blue-500" style={{ animationDuration: "3s" }} />
        <p className="text-muted-foreground">
          <span className="font-medium text-foreground">Warming up</span>
          {" — "}
          Azure Functions cold start in progress. This typically takes ~30 seconds
          on the free tier.
        </p>
      </div>
    </div>
  );
}
