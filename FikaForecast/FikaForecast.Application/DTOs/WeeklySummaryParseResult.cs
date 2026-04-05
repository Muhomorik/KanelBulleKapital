using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Result of parsing the Weekly Summary Agent's JSON output into domain objects.
/// Contains mood, themes, completeness flag, and any warnings.
/// </summary>
public sealed record WeeklySummaryParseResult(
    MarketSentiment NetMood,
    string MoodSummary,
    IReadOnlyList<WeeklySummaryTheme> Themes,
    bool IsComplete,
    IReadOnlyList<string> Warnings);
