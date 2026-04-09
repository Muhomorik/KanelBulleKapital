import type { SignalStrength } from "@/lib/types";
import { Zap, Minus, ChevronDown } from "lucide-react";

const config: Record<
  SignalStrength,
  { label: string; className: string; icon: typeof Zap }
> = {
  Strong: {
    label: "Strong",
    className: "bg-signal-strong/10 text-signal-strong border-signal-strong/20",
    icon: Zap,
  },
  Moderate: {
    label: "Moderate",
    className:
      "bg-signal-moderate/10 text-signal-moderate border-signal-moderate/20",
    icon: Minus,
  },
  Weak: {
    label: "Weak",
    className: "bg-signal-weak/10 text-signal-weak border-signal-weak/20",
    icon: ChevronDown,
  },
};

interface SignalBadgeProps {
  strength: SignalStrength;
}

export function SignalBadge({ strength }: SignalBadgeProps) {
  const { label, className, icon: Icon } = config[strength];

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {label}
    </span>
  );
}
