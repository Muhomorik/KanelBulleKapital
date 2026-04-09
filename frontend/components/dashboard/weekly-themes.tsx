import type { WeeklySummaryRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { SentimentBadge } from "./sentiment-badge";
import { ConfidenceBadge } from "./confidence-badge";
import { CalendarDays, Layers } from "lucide-react";

interface WeeklyThemesProps {
  data: WeeklySummaryRun | null;
}

export function WeeklyThemes({ data }: WeeklyThemesProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-2">
        <SectionHeader />
        <EmptyCard message="No weekly summary available." />
      </section>
    );
  }

  const weekLabel = `${formatDate(data.weekStart)} — ${formatDate(data.weekEnd)}`;

  return (
    <section className="animate-fade-up stagger-2">
      <SectionHeader />

      <Card className="mb-4">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="font-serif text-lg">Weekly Mood</CardTitle>
            <SentimentBadge sentiment={data.netMood} size="lg" />
          </div>
          <CardDescription className="flex items-center gap-1 text-xs">
            <CalendarDays className="h-3 w-3" />
            {weekLabel}
          </CardDescription>
        </CardHeader>
      </Card>

      <div className="space-y-3">
        {(data.themes ?? []).map((theme, i) => (
          <Card key={theme.theme} className={`animate-fade-up stagger-${i + 3}`}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-2">
                <CardTitle className="text-sm font-semibold">
                  {theme.theme}
                </CardTitle>
                <ConfidenceBadge level={theme.confidence} />
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-xs leading-relaxed text-muted-foreground">
                {theme.description}
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
        <Layers className="h-4 w-4" />
      </div>
      <div>
        <h2 className="font-serif text-xl font-semibold tracking-tight">
          Weekly Themes
        </h2>
        <p className="text-xs text-muted-foreground">
          Aggregated market themes
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

function formatDate(dateStr: string): string {
  const d = new Date(dateStr + "T00:00:00");
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}
