using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>Recurring theme extracted from a week of daily briefs. Serialized as JSON into Azure Tables.</summary>
public class WeeklySummaryTheme
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public ConfidenceLevel Confidence { get; set; }

    [JsonPropertyName("sentiment")]
    public MarketSentiment Sentiment { get; set; }
}
