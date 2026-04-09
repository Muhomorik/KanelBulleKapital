import type { MarketSentiment } from "@/lib/types";
import { TrendingUp, TrendingDown, ArrowLeftRight } from "lucide-react";

const config: Record<
  MarketSentiment,
  { label: string; className: string; icon: typeof TrendingUp }
> = {
  RiskOn: {
    label: "Risk-On",
    className: "bg-risk-on/10 text-risk-on border-risk-on/20",
    icon: TrendingUp,
  },
  RiskOff: {
    label: "Risk-Off",
    className: "bg-risk-off/10 text-risk-off border-risk-off/20",
    icon: TrendingDown,
  },
  Mixed: {
    label: "Mixed",
    className: "bg-mixed/10 text-mixed border-mixed/20",
    icon: ArrowLeftRight,
  },
};

interface SentimentBadgeProps {
  sentiment: MarketSentiment;
  size?: "sm" | "md" | "lg";
}

export function SentimentBadge({ sentiment, size = "md" }: SentimentBadgeProps) {
  const { label, className, icon: Icon } = config[sentiment];

  const sizeClasses = {
    sm: "px-2 py-0.5 text-xs gap-1",
    md: "px-2.5 py-1 text-xs gap-1.5",
    lg: "px-3 py-1.5 text-sm gap-2",
  };

  const iconSizes = {
    sm: "h-3 w-3",
    md: "h-3.5 w-3.5",
    lg: "h-4 w-4",
  };

  return (
    <span
      className={`inline-flex items-center rounded-full border font-medium ${className} ${sizeClasses[size]}`}
    >
      <Icon className={iconSizes[size]} />
      {label}
    </span>
  );
}
