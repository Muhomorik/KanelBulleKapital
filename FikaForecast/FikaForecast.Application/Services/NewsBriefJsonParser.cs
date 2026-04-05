using System.Text.Json;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Deterministic JSON parser that extracts structured data from
/// the News Brief Agent's JSON output.
/// </summary>
public class NewsBriefJsonParser : INewsBriefParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger _logger;

    public NewsBriefJsonParser(ILogger logger)
    {
        _logger = logger;
    }

    public NewsBriefParseResult Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return new NewsBriefParseResult(null, false, ["Empty JSON input"]);

        var warnings = new List<string>();

        NewsBriefOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<NewsBriefOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize agent JSON output");
            return new NewsBriefParseResult(null, false, [$"JSON deserialization failed: {ex.Message}"]);
        }

        if (output is null)
            return new NewsBriefParseResult(null, false, ["Deserialized output was null"]);

        var mood = ResolveSentiment(output.Mood, warnings, "mood");
        var item = new NewsItem(mood, output.Summary ?? string.Empty);

        if (output.Categories is { Count: > 0 })
        {
            foreach (var cat in output.Categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Headline))
                {
                    warnings.Add($"Skipping category item with empty headline in '{cat.Category}'");
                    continue;
                }

                var sentiment = ResolveSentiment(cat.Sentiment, warnings, $"category '{cat.Category}'");

                item.AddAssessment(new CategoryAssessment(
                    category: cat.Category ?? string.Empty,
                    headline: cat.Headline,
                    summary: cat.Summary ?? string.Empty,
                    sentiment: sentiment));
            }
        }
        else
        {
            warnings.Add("No categories found in JSON output");
        }

        var isComplete = warnings.Count == 0 && item.Assessments.Count > 0;

        _logger.Debug("Parsed {0} assessments (complete: {1})", item.Assessments.Count, isComplete);

        if (warnings.Count > 0)
            _logger.Warn("Parse warnings: {0}", string.Join("; ", warnings));

        return new NewsBriefParseResult(item, isComplete, warnings);
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
}
