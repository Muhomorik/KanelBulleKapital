using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Capital rotation path from one sector to another. Serialized as JSON into Azure Tables.</summary>
public class RotationChain
{
    [JsonPropertyName("capitalFleeing")]
    public string CapitalFleeing { get; set; } = string.Empty;

    [JsonPropertyName("flowsToward")]
    public string FlowsToward { get; set; } = string.Empty;

    [JsonPropertyName("mechanism")]
    public string Mechanism { get; set; } = string.Empty;
}
