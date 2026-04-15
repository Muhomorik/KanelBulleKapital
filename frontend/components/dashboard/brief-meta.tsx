"use client";

import { useSyncExternalStore } from "react";
import { Clock, Cpu } from "lucide-react";

interface BriefMetaData {
  createdAt: string;
  modelId: string;
  durationSeconds: number;
  totalTokens: number;
}

export function BriefMeta({ data }: { data: BriefMetaData }) {
  return (
    <>
      <BriefTimestamp iso={data.createdAt} />
      <span className="inline-flex items-center gap-1">
        <Cpu className="h-3.5 w-3.5" />
        {data.modelId}
      </span>
      <span className="inline-flex items-center gap-1">
        <Clock className="h-3.5 w-3.5" />
        {(data.durationSeconds ?? 0).toFixed(1)}s
      </span>
      <span>{(data.totalTokens ?? 0).toLocaleString("en-US")} tokens</span>
    </>
  );
}

// SSR renders a UTC-labeled string; client swaps to local time after hydration
// to avoid mismatch. See frontend/components/footer.tsx for the same pattern.
const noopSubscribe = () => () => {};

export function BriefTimestamp({ iso }: { iso: string }) {
  const mode = useSyncExternalStore(
    noopSubscribe,
    () => "local" as const,
    () => "utc" as const,
  );
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return null;
  const label =
    mode === "local"
      ? d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
      : `${String(d.getUTCHours()).padStart(2, "0")}:${String(d.getUTCMinutes()).padStart(2, "0")} UTC`;
  return (
    <span className="font-mono uppercase tracking-[0.12em] tabular-nums text-foreground/80">
      {label}
    </span>
  );
}
