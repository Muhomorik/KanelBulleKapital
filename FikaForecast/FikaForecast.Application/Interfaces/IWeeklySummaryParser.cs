using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Deterministic parser that extracts structured weekly summary themes
/// from the Weekly Summary Agent's raw JSON output.
/// </summary>
public interface IWeeklySummaryParser
{
    /// <summary>
    /// Parses raw JSON into mood, themes, and confidence levels.
    /// Resilient to partial failures — returns whatever was successfully parsed.
    /// </summary>
    WeeklySummaryParseResult Parse(string rawJson);
}
