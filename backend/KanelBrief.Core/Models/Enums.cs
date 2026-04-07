using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Outcome of an agent run.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunStatus
{
    Success,
    Failed,
    Partial
}

/// <summary>Overall market mood direction.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MarketSentiment
{
    RiskOn,
    RiskOff,
    Mixed
}

/// <summary>How confident an agent is in a theme assessment.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConfidenceLevel
{
    High,
    Medium,
    Low
}

/// <summary>Strength of a rotation opportunity signal.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalStrength
{
    Strong,
    Moderate,
    Weak
}
