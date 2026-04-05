using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Deterministic parser that extracts structured news items and market mood
/// from the News Brief Agent's raw JSON output.
/// </summary>
public interface INewsBriefParser
{
    /// <summary>
    /// Parses raw JSON into a <see cref="NewsItem"/> with <see cref="CategoryAssessment"/> children.
    /// Resilient to partial failures — returns whatever was successfully parsed.
    /// </summary>
    NewsBriefParseResult Parse(string rawJson);
}
