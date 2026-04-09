import type { NewsBriefRun } from "@/lib/types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { SentimentBadge } from "./sentiment-badge";
import { Newspaper, Clock, Cpu } from "lucide-react";

interface MarketPulseProps {
  data: NewsBriefRun | null;
}

export function MarketPulse({ data }: MarketPulseProps) {
  if (!data) {
    return (
      <section className="animate-fade-up stagger-1">
        <SectionHeader
          title="Market Pulse"
          subtitle="Daily news brief"
          icon={<Newspaper className="h-4 w-4" />}
        />
        <EmptyState message="No news brief available for this date." />
      </section>
    );
  }

  return (
    <section className="animate-fade-up stagger-1">
      <SectionHeader
        title="Market Pulse"
        subtitle="Daily news brief"
        icon={<Newspaper className="h-4 w-4" />}
      />

      {/* Mood + Summary */}
      <Card className="mb-4">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="font-serif text-lg">
              Today&apos;s Mood
            </CardTitle>
            <SentimentBadge sentiment={data.mood} size="lg" />
          </div>
          <CardDescription className="flex items-center gap-3 text-xs">
            <span className="inline-flex items-center gap-1">
              <Cpu className="h-3 w-3" />
              {data.modelId}
            </span>
            <span className="inline-flex items-center gap-1">
              <Clock className="h-3 w-3" />
              {(data.durationSeconds ?? 0).toFixed(1)}s
            </span>
            <span>{(data.totalTokens ?? 0).toLocaleString()} tokens</span>
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm leading-relaxed text-muted-foreground">
            {data.summary}
          </p>
        </CardContent>
      </Card>

      {/* Category Assessments */}
      <div className="grid gap-3 sm:grid-cols-2">
        {data.assessments.map((assessment, i) => (
          <Card key={assessment.category} className={`animate-fade-up stagger-${i + 2}`}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-2">
                <CardTitle className="text-sm font-semibold">
                  {assessment.category}
                </CardTitle>
                <SentimentBadge sentiment={assessment.sentiment} size="sm" />
              </div>
              <CardDescription className="text-xs font-medium">
                {assessment.headline}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-xs leading-relaxed text-muted-foreground">
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
    <div className="mb-4 flex items-center gap-2.5">
      <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary/10 text-primary">
        {icon}
      </div>
      <div>
        <h2 className="font-serif text-xl font-semibold tracking-tight">
          {title}
        </h2>
        <p className="text-xs text-muted-foreground">{subtitle}</p>
      </div>
    </div>
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
