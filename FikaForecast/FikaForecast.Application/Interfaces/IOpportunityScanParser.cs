using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Deterministic parser that extracts structured rotation targets
/// from the Opportunity Scan Agent's raw JSON output.
/// </summary>
public interface IOpportunityScanParser
{
    /// <summary>
    /// Parses raw JSON into rotation target entries.
    /// Resilient to partial failures — returns whatever was successfully parsed.
    /// </summary>
    OpportunityScanParseResult Parse(string rawJson);
}
