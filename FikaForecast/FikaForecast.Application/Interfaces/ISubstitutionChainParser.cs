using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Deterministic parser that extracts structured rotation chains
/// from the Substitution Chain Agent's raw JSON output.
/// </summary>
public interface ISubstitutionChainParser
{
    /// <summary>
    /// Parses raw JSON into rotation chain entries.
    /// Resilient to partial failures — returns whatever was successfully parsed.
    /// </summary>
    SubstitutionChainParseResult Parse(string rawJson);
}
