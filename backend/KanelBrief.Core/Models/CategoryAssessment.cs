using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Single sector assessment from a News Brief run. Serialized as JSON into Azure Tables.</summary>
public class CategoryAssessment
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("sentiment")]
    public MarketSentiment Sentiment { get; set; }
}
