import type {
  OpportunityScanRun,
  RotationTarget,
  SignalStrength,
} from "@/lib/types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { SignalBadge } from "./signal-badge";
import { BriefMeta } from "./brief-meta";
import { Target, Gem } from "lucide-react";

export const OPPORTUNITIES_PANEL_ID = "opportunities";

interface OpportunitiesProps {
  data: OpportunityScanRun | null;
  /** Render placeholder skeletons instead of content while data is loading. */
  loading?: boolean;
}

const GROUP_ORDER: SignalStrength[] = ["Strong", "Moderate", "Weak"];
const GROUP_LABEL: Record<SignalStrength, string> = {
  Strong: "Strong conviction",
  Moderate: "Moderate conviction",
  Weak: "Weak conviction",
};

export function Opportunities({ data, loading }: OpportunitiesProps) {
  if (loading) {
    return (
      <section id={OPPORTUNITIES_PANEL_ID} className="animate-fade-up stagger-4 scroll-mt-20">
        <SectionHeader />
        <OpportunitiesSkeleton />
      </section>
    );
  }

  if (!data) {
    return (
      <section id={OPPORTUNITIES_PANEL_ID} className="animate-fade-up stagger-4 scroll-mt-20">
        <SectionHeader />
        <EmptyCard message="No opportunity scans available." />
      </section>
    );
  }

  const targets = data.targets ?? [];
  const grouped = GROUP_ORDER.map((strength) => ({
    strength,
    items: targets.filter((t) => t.signalStrength === strength),
  })).filter((group) => group.items.length > 0);

  // Flat-index offset for each group — lets us compute per-card stagger classes
  // as a pure function of (groupI, itemI) without mutating a closure-captured
  // counter during render (react-hooks/immutability).
  const groupStartIndices = grouped.map((_, i) =>
    grouped.slice(0, i).reduce((sum, g) => sum + g.items.length, 0),
  );

  return (
    <section id={OPPORTUNITIES_PANEL_ID} className="animate-fade-up stagger-4 scroll-mt-20">
      <SectionHeader />

      <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
        <BriefMeta data={data} />
      </div>

      <div className="space-y-6">
        {grouped.map(({ strength, items }, groupI) => (
          <div key={strength}>
            <p className="mb-2 flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.14em] text-foreground">
              <span>{GROUP_LABEL[strength]}</span>
              <span className="h-px flex-1 bg-border" aria-hidden="true" />
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {items.map((target, itemI) => {
                const flatIndex = groupStartIndices[groupI] + itemI;
                const stagger = `stagger-${Math.min(flatIndex + 5, 9)}`;
                return (
                  <TargetCard
                    key={target.category}
                    target={target}
                    staggerClass={stagger}
                  />
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function TargetCard({
  target,
  staggerClass,
}: {
  target: RotationTarget;
  staggerClass: string;
}) {
  return (
    <Card className={`animate-fade-up ${staggerClass}`}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-3">
          <CardTitle className="flex items-center gap-2.5 font-serif text-2xl font-bold leading-tight tracking-tight text-foreground">
            <Gem
              aria-hidden="true"
              className="h-5 w-5 shrink-0 text-primary"
            />
            <span>{target.category}</span>
          </CardTitle>
          <SignalBadge strength={target.signalStrength} />
        </div>
        <CardDescription className="mt-3 text-sm leading-relaxed text-foreground/85">
          {target.rationale}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div
          role="note"
          aria-label="Risk caveat"
          className="border-t border-border pt-3"
        >
          <p className="mb-1.5 text-xs font-bold uppercase tracking-[0.14em] text-destructive">
            Risk
          </p>
          <p className="text-sm leading-relaxed text-foreground/85">
            {target.riskCaveat}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

function SectionHeader() {
  return (
    <div className="mb-8 border-y border-foreground/30 py-6">
      <div className="flex items-center gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-md bg-primary/15 text-primary">
          <Target className="h-6 w-6" />
        </div>
        <div>
          <h2 className="font-serif text-4xl font-bold uppercase leading-none tracking-tight">
            Opportunities
          </h2>
          <p className="mt-2 text-sm font-semibold uppercase tracking-[0.14em] text-muted-foreground">
            Actionable rotation targets
          </p>
        </div>
      </div>
    </div>
  );
}

function EmptyCard({ message }: { message: string }) {
  return (
    <Card className="border-dashed">
      <CardContent className="py-8 text-center text-sm text-muted-foreground">
        {message}
      </CardContent>
    </Card>
  );
}

function OpportunitiesSkeleton() {
  const groups = [
    { label: GROUP_LABEL.Strong, count: 2 },
    { label: GROUP_LABEL.Moderate, count: 2 },
  ];
  return (
    <>
      <div
        className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1"
        aria-hidden="true"
      >
        <Skeleton className="h-3.5 w-20" />
        <Skeleton className="h-3.5 w-24" />
        <Skeleton className="h-3.5 w-16" />
      </div>
      <div className="space-y-6" aria-hidden="true">
        {groups.map(({ label, count }) => (
          <div key={label}>
            <p className="mb-2 flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.14em] text-foreground">
              <span>{label}</span>
              <span className="h-px flex-1 bg-border" aria-hidden="true" />
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {Array.from({ length: count }).map((_, i) => (
                <Card key={i}>
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between gap-3">
                      <Skeleton className="h-7 w-32" />
                      <Skeleton className="h-5 w-16 rounded-full" />
                    </div>
                    <div className="mt-3 space-y-2">
                      <Skeleton className="h-3.5 w-full" />
                      <Skeleton className="h-3.5 w-5/6" />
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="border-t border-border pt-3">
                      <Skeleton className="mb-2 h-3 w-10" />
                      <Skeleton className="h-3.5 w-4/5" />
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>
        ))}
      </div>
    </>
  );
}
