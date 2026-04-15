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
import { BriefMeta } from "./brief-meta";
import { Bookmark, Layers } from "lucide-react";

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

  return (
    <section className="animate-fade-up stagger-2">
      <SectionHeader />

      <Card className="mb-4">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between gap-3">
            <CardTitle className="flex items-center gap-2.5 font-serif text-2xl font-bold leading-tight tracking-tight text-foreground">
              <Bookmark
                aria-hidden="true"
                className="h-5 w-5 shrink-0 text-primary"
              />
              <span>Weekly Mood</span>
            </CardTitle>
            <SentimentBadge sentiment={data.netMood} size="lg" />
          </div>
          <CardDescription className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
            <BriefMeta data={data} />
          </CardDescription>
        </CardHeader>
        {data.moodSummary && (
          <CardContent>
            <p className="text-sm leading-relaxed text-muted-foreground">
              {data.moodSummary}
            </p>
          </CardContent>
        )}
      </Card>

      <div className="grid gap-3 sm:grid-cols-2">
        {(data.themes ?? []).map((theme, i) => (
          <Card
            key={theme.category}
            className={`animate-fade-up stagger-${i + 3}`}
          >
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-3">
                <CardTitle className="flex items-center gap-2.5 font-serif text-2xl font-bold leading-tight tracking-tight text-foreground">
                  <Bookmark
                    aria-hidden="true"
                    className="h-5 w-5 shrink-0 text-primary"
                  />
                  <span>{theme.category}</span>
                </CardTitle>
                <div className="flex shrink-0 items-center gap-1.5">
                  <SentimentBadge sentiment={theme.sentiment} size="sm" />
                  <ConfidenceBadge level={theme.confidence} />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-relaxed text-foreground/85">
                {theme.summary}
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
          <Layers className="h-6 w-6" />
        </div>
        <div>
          <h2 className="font-serif text-4xl font-bold uppercase leading-none tracking-tight">
            Weekly Themes
          </h2>
          <p className="mt-2 text-sm font-semibold uppercase tracking-[0.14em] text-muted-foreground">
            Aggregated market themes
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

