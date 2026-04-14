import type { SubstitutionChainRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardHeader,
} from "@/components/ui/card";
import { BriefMeta } from "./brief-meta";
import { ArrowRight, GitBranch } from "lucide-react";

interface CapitalFlowsProps {
  data: SubstitutionChainRun | null;
}

export function CapitalFlows({ data }: CapitalFlowsProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-3">
        <SectionHeader />
        <EmptyCard message="No capital rotation data available." />
      </section>
    );
  }

  return (
    <section className="animate-fade-up stagger-3">
      <SectionHeader />

      <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
        <BriefMeta data={data} />
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        {(data.chains ?? []).map((chain) => (
          <Card
            key={`${chain.capitalFleeing}→${chain.flowsToward}`}
            className="animate-fade-up"
          >
            <CardHeader className="pb-3">
              <div className="flex flex-wrap items-end gap-2 text-sm">
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground">
                    Out of
                  </span>
                  <span className="inline-flex items-center rounded-md border border-risk-off/60 bg-card px-2.5 py-1 font-bold text-risk-off">
                    {chain.capitalFleeing}
                  </span>
                </div>
                <ArrowRight
                  aria-hidden="true"
                  className="mb-2 h-4 w-4 shrink-0 text-muted-foreground"
                />
                <div className="flex flex-col gap-1">
                  <span className="text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground">
                    Into
                  </span>
                  <span className="inline-flex items-center rounded-md border border-risk-on/60 bg-card px-2.5 py-1 font-bold text-risk-on">
                    {chain.flowsToward}
                  </span>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-relaxed text-foreground">
                {chain.mechanism}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </section>
  );
}

function SectionHeader() {
  return (
    <div className="mb-8 border-y border-foreground/30 py-6">
      <div className="flex items-center gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-md bg-primary/15 text-primary">
          <GitBranch className="h-6 w-6" />
        </div>
        <div>
          <h2 className="font-serif text-4xl font-bold uppercase leading-none tracking-tight">
            Capital Flows
          </h2>
          <p className="mt-2 text-sm font-semibold uppercase tracking-[0.14em] text-muted-foreground">
            Substitution chain rotation paths
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
