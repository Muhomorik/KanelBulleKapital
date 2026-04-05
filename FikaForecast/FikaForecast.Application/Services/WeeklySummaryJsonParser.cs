using System.Text.Json;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Deterministic JSON parser that extracts structured themes from
/// the Weekly Summary Agent's JSON output.
/// </summary>
public class WeeklySummaryJsonParser : IWeeklySummaryParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger _logger;

    public WeeklySummaryJsonParser(ILogger logger)
    {
        _logger = logger;
    }

    public WeeklySummaryParseResult Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return new WeeklySummaryParseResult(
                MarketSentiment.Mixed, string.Empty, [], false, ["Empty JSON input"]);

        var warnings = new List<string>();

        WeeklySummaryOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<WeeklySummaryOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize weekly summary JSON output");
            return new WeeklySummaryParseResult(
                MarketSentiment.Mixed, string.Empty, [], false,
                [$"JSON deserialization failed: {ex.Message}"]);
        }

        if (output is null)
            return new WeeklySummaryParseResult(
                MarketSentiment.Mixed, string.Empty, [], false, ["Deserialized output was null"]);

        var netMood = ResolveSentiment(output.NetMood, warnings, "net_mood");
        var moodSummary = output.MoodSummary ?? string.Empty;
        var themes = new List<WeeklySummaryTheme>();

        if (output.Themes is { Count: > 0 })
        {
            foreach (var t in output.Themes)
            {
                if (string.IsNullOrWhiteSpace(t.Category))
                {
                    warnings.Add("Skipping theme with empty category");
                    continue;
                }

                var confidence = ResolveConfidence(t.Confidence, warnings, $"theme '{t.Category}'");
                var sentiment = ResolveSentiment(t.Sentiment, warnings, $"theme '{t.Category}'");

                themes.Add(new WeeklySummaryTheme(
                    category: t.Category,
                    summary: t.Summary ?? string.Empty,
                    confidence: confidence,
                    sentiment: sentiment));
            }
        }
        else
        {
            warnings.Add("No themes found in JSON output");
        }

        var isComplete = warnings.Count == 0 && themes.Count > 0;

        _logger.Debug("Parsed {0} themes (complete: {1})", themes.Count, isComplete);

        if (warnings.Count > 0)
            _logger.Warn("Parse warnings: {0}", string.Join("; ", warnings));

        return new WeeklySummaryParseResult(netMood, moodSummary, themes, isComplete, warnings);
    }

    private static MarketSentiment ResolveSentiment(string? value, List<string> warnings, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            warnings.Add($"Missing sentiment for {context}, defaulting to Mixed");
            return MarketSentiment.Mixed;
        }

        if (Enum.TryParse<MarketSentiment>(value, ignoreCase: true, out var sentiment))
            return sentiment;

        warnings.Add($"Unknown sentiment '{value}' for {context}, defaulting to Mixed");
        return MarketSentiment.Mixed;
    }

    private static ConfidenceLevel ResolveConfidence(string? value, List<string> warnings, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            warnings.Add($"Missing confidence for {context}, defaulting to Dropped");
            return ConfidenceLevel.Dropped;
        }

        if (Enum.TryParse<ConfidenceLevel>(value, ignoreCase: true, out var confidence))
            return confidence;

        warnings.Add($"Unknown confidence '{value}' for {context}, defaulting to Dropped");
        return ConfidenceLevel.Dropped;
    }
}
