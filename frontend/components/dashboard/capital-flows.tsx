import type { SubstitutionChainRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
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

      <div className="space-y-3">
        {(data.chains ?? []).map((chain, i) => (
          <Card key={i} className={`animate-fade-up stagger-${i + 4}`}>
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2 text-sm">
                <span className="rounded-md bg-risk-off/10 px-2.5 py-1 font-medium text-risk-off">
                  {chain.capitalFleeing}
                </span>
                <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                <span className="rounded-md bg-risk-on/10 px-2.5 py-1 font-medium text-risk-on">
                  {chain.flowsToward}
                </span>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-xs leading-relaxed text-muted-foreground">
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
    <div className="mb-4 flex items-center gap-2.5">
      <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary/10 text-primary">
        <GitBranch className="h-4 w-4" />
      </div>
      <div>
        <h2 className="font-serif text-xl font-semibold tracking-tight">
          Capital Flows
        </h2>
        <p className="text-xs text-muted-foreground">
          Substitution chain rotation paths
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
