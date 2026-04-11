using KanelBrief.Core.Models;

namespace KanelBrief.Core.Parsers;

/// <summary>
/// Parses raw LLM agent responses into domain types.
/// Centralises JSON extraction and enum parsing that was previously duplicated across agents.
/// </summary>
public static class AgentResponseParser
{
    /// <summary>
    /// Extracts the first JSON object from an LLM response that may contain surrounding text,
    /// markdown fences, or other non-JSON content.
    /// </summary>
    public static string ExtractJson(string text)
    {
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("No JSON found in agent response");

        return text[startIndex..(endIndex + 1)];
    }

    /// <summary>Parses a sentiment string (case-insensitive) to <see cref="MarketSentiment"/>. Defaults to Mixed.</summary>
    public static MarketSentiment ParseSentiment(string sentiment)
    {
        return sentiment.ToLowerInvariant() switch
        {
            "riskon" => MarketSentiment.RiskOn,
            "riskoff" => MarketSentiment.RiskOff,
            _ => MarketSentiment.Mixed
        };
    }

    /// <summary>Parses a confidence string (case-insensitive) to <see cref="ConfidenceLevel"/>. Defaults to Medium.</summary>
    public static ConfidenceLevel ParseConfidence(string confidence)
    {
        return confidence.ToLowerInvariant() switch
        {
            "high" => ConfidenceLevel.High,
            "medium" => ConfidenceLevel.Medium,
            "low" => ConfidenceLevel.Low,
            _ => ConfidenceLevel.Medium
        };
    }

    /// <summary>Parses a signal strength string (case-insensitive) to <see cref="SignalStrength"/>. Defaults to Moderate.</summary>
    public static SignalStrength ParseSignalStrength(string strength)
    {
        return strength.ToLowerInvariant() switch
        {
            "strong" => SignalStrength.Strong,
            "moderate" => SignalStrength.Moderate,
            "weak" => SignalStrength.Weak,
            _ => SignalStrength.Moderate
        };
    }
}
