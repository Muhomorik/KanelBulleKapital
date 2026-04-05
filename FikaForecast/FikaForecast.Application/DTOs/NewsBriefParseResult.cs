using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Result of parsing the News Brief Agent's JSON output into domain objects.
/// Contains the parsed item (with assessments), completeness flag, and any warnings.
/// </summary>
public sealed record NewsBriefParseResult(
    NewsItem? Item,
    bool IsComplete,
    IReadOnlyList<string> Warnings);
