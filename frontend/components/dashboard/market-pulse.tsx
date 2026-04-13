"use client";

import { useSyncExternalStore } from "react";
import type { NewsBriefRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { SentimentBadge } from "./sentiment-badge";
import { Activity, Newspaper, Clock, Cpu } from "lucide-react";

export const MARKET_PULSE_PANEL_ID = "market-pulse-panel";

interface MarketPulseProps {
  data: NewsBriefRun | null;
  /** Optional toolbar rendered above the brief — typically a <BriefSelector />. */
  selector?: React.ReactNode;
}

export function MarketPulse({ data, selector }: MarketPulseProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-1" id={MARKET_PULSE_PANEL_ID}>
        <SectionHeader
          title="Market Pulse"
          subtitle="News brief · every 4 hours"
          icon={<Newspaper className="h-6 w-6" />}
        />
        {selector}
        <EmptyState message="No news brief available for this date." />
      </section>
    );
  }

  return (
    <section
      className="animate-fade-up stagger-1"
      id={MARKET_PULSE_PANEL_ID}
      role="tabpanel"
      aria-labelledby={`${MARKET_PULSE_PANEL_ID}-title`}
    >
      <SectionHeader
        title="Market Pulse"
        subtitle="News brief · every 4 hours"
        icon={<Newspaper className="h-6 w-6" />}
      />
      {selector}

      {/* Mood + Summary */}
      <Card className="mb-4">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between gap-3">
            <CardTitle className="flex items-center gap-2.5 font-serif text-2xl font-bold leading-tight tracking-tight text-foreground">
              <Activity
                aria-hidden="true"
                className="h-5 w-5 shrink-0 text-primary"
              />
              <span>Today&apos;s Mood</span>
            </CardTitle>
            <SentimentBadge sentiment={data.mood} size="lg" />
          </div>
          <CardDescription className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
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
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm leading-relaxed text-foreground/85">
            {data.summary}
          </p>
        </CardContent>
      </Card>

      {/* Category Assessments */}
      <div className="grid gap-3 sm:grid-cols-2">
        {(data.assessments ?? []).map((assessment, i) => (
          <Card key={assessment.category} className={`animate-fade-up stagger-${i + 2}`}>
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between gap-3">
                <CardTitle className="flex items-center gap-2.5 font-serif text-2xl font-bold leading-tight tracking-tight text-foreground">
                  <Activity
                    aria-hidden="true"
                    className="h-5 w-5 shrink-0 text-primary"
                  />
                  <span>{assessment.category}</span>
                </CardTitle>
                <SentimentBadge sentiment={assessment.sentiment} size="sm" />
              </div>
              <CardDescription className="mt-2 text-sm font-semibold text-foreground/90">
                {assessment.headline}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-relaxed text-foreground/85">
                {assessment.summary}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </section>
  );
}

function SectionHeader({
  title,
  subtitle,
  icon,
}: {
  title: string;
  subtitle: string;
  icon: React.ReactNode;
}) {
  return (
    <div className="mb-8 border-y border-foreground/30 py-6">
      <div className="flex items-center gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-md bg-primary/15 text-primary">
          {icon}
        </div>
        <div>
          <h2 className="font-serif text-4xl font-bold uppercase leading-none tracking-tight">
            {title}
          </h2>
          <p className="mt-2 text-sm font-semibold uppercase tracking-[0.14em] text-muted-foreground">
            {subtitle}
          </p>
        </div>
      </div>
    </div>
  );
}

// SSR renders a UTC-labeled string; client swaps to local time after hydration
// to avoid mismatch. See frontend/components/footer.tsx for the same pattern.
const noopSubscribe = () => () => {};

function BriefTimestamp({ iso }: { iso: string }) {
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

function EmptyState({ message }: { message: string }) {
  return (
    <Card className="border-dashed">
      <CardContent className="py-8 text-center text-sm text-muted-foreground">
        {message}
      </CardContent>
    </Card>
  );
}
