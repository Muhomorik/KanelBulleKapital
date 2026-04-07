using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Top rotation opportunity identified by the Opportunity Scan agent. Serialized as JSON into Azure Tables.</summary>
public class RotationTarget
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("signalStrength")]
    public SignalStrength SignalStrength { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [JsonPropertyName("riskCaveat")]
    public string RiskCaveat { get; set; } = string.Empty;
}
