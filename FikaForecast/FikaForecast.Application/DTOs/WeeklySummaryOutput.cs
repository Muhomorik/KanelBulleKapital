using System.Text.Json.Serialization;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// JSON deserialization target for the Weekly Summary Agent's structured output.
/// </summary>
public sealed record WeeklySummaryOutput(
    [property: JsonPropertyName("net_mood")] string NetMood,
    [property: JsonPropertyName("mood_summary")] string MoodSummary,
    List<WeeklySummaryOutputTheme> Themes);

/// <summary>
/// A single consolidated theme from the agent's JSON output.
/// </summary>
public sealed record WeeklySummaryOutputTheme(
    string Category,
    string Summary,
    string Confidence,
    string Sentiment);
