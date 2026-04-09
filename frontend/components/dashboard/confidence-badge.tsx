import type { ConfidenceLevel } from "@/lib/types";

const config: Record<ConfidenceLevel, { label: string; dots: number }> = {
  High: { label: "High", dots: 3 },
  Medium: { label: "Med", dots: 2 },
  Low: { label: "Low", dots: 1 },
};

interface ConfidenceBadgeProps {
  level: ConfidenceLevel;
}

export function ConfidenceBadge({ level }: ConfidenceBadgeProps) {
  const { label, dots } = config[level] ?? config.Medium;

  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
      <span className="flex gap-0.5">
        {[1, 2, 3].map((i) => (
          <span
            key={i}
            className={`h-1.5 w-1.5 rounded-full ${
              i <= dots ? "bg-primary" : "bg-border"
            }`}
          />
        ))}
      </span>
      {label}
    </span>
  );
}
