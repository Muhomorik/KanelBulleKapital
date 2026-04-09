import type { OpportunityScanRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { SignalBadge } from "./signal-badge";
import { Target, AlertTriangle } from "lucide-react";

interface OpportunitiesProps {
  data: OpportunityScanRun | null;
}

export function Opportunities({ data }: OpportunitiesProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-4">
        <SectionHeader />
        <EmptyCard message="No opportunity scans available." />
      </section>
    );
  }

  return (
    <section className="animate-fade-up stagger-4">
      <SectionHeader />

      <div className="space-y-3">
        {(data.targets ?? []).map((target, i) => (
          <Card key={i} className={`animate-fade-up stagger-${i + 5}`}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-2">
                <CardTitle className="text-sm font-semibold">
                  {target.category}
                </CardTitle>
                <SignalBadge strength={target.signalStrength} />
              </div>
              <CardDescription className="text-xs leading-relaxed">
                {target.rationale}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex items-start gap-1.5 rounded-md bg-destructive/5 px-2.5 py-2 text-xs text-muted-foreground">
                <AlertTriangle className="mt-0.5 h-3 w-3 shrink-0 text-destructive/70" />
                <span>{target.riskCaveat}</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </section>
  );
}

function SectionHeader() {
  return (
    <div className="mb-4 flex items-center gap-2.5">
      <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary/10 text-primary">
        <Target className="h-4 w-4" />
      </div>
      <div>
        <h2 className="font-serif text-xl font-semibold tracking-tight">
          Opportunities
        </h2>
        <p className="text-xs text-muted-foreground">
          Actionable rotation targets
        </p>
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
