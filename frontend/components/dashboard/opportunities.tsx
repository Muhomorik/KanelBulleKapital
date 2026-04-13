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
import { SignalBadge } from "./signal-badge";
import { Target, Gem } from "lucide-react";

interface OpportunitiesProps {
  data: OpportunityScanRun | null;
}

const GROUP_ORDER: SignalStrength[] = ["Strong", "Moderate", "Weak"];
const GROUP_LABEL: Record<SignalStrength, string> = {
  Strong: "Strong conviction",
  Moderate: "Moderate conviction",
  Weak: "Weak conviction",
};

export function Opportunities({ data }: OpportunitiesProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-4">
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

  let cardIndex = 0;

  return (
    <section className="animate-fade-up stagger-4">
      <SectionHeader />

      <div className="space-y-6">
        {grouped.map(({ strength, items }) => (
          <div key={strength}>
            <p className="mb-2 flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.14em] text-foreground">
              <span>{GROUP_LABEL[strength]}</span>
              <span className="h-px flex-1 bg-border" aria-hidden="true" />
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {items.map((target) => {
                const stagger = `stagger-${Math.min(cardIndex + 5, 9)}`;
                cardIndex += 1;
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
