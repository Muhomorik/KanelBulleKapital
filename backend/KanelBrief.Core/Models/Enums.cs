using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Outcome of an agent run.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunStatus
{
    /// <summary>The run completed end-to-end and its result was persisted.</summary>
    Success,

    /// <summary>The run failed. Reserved for future use — current code does not write this value;
    /// failures are logged and propagated without persisting anything.</summary>
    Failed,

    /// <summary>
    /// Historically used when an LLM call failed and the agent saved a row with fabricated
    /// fallback data.
    /// <b>Do not write this value in new code.</b>
    /// The enum member is kept so historical Azure Tables rows with <c>Status = "Partial"</c>
    /// still deserialize; it will be removed after those rows are cleaned up.
    /// </summary>
    [Obsolete("Partial runs are no longer written. Failures propagate without persisting. " +
              "Retained only to deserialize historical rows.")]
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