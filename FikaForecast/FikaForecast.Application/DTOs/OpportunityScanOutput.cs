using System.Text.Json.Serialization;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// JSON deserialization target for the Opportunity Scan Agent's structured output.
/// </summary>
public sealed record OpportunityScanOutput(
    List<OpportunityScanOutputTarget> Targets);

/// <summary>
/// A single rotation target entry from the agent's JSON output.
/// </summary>
public sealed record OpportunityScanOutputTarget(
    string Category,
    [property: JsonPropertyName("signal_strength")] string SignalStrength,
    string Rationale,
    [property: JsonPropertyName("risk_caveat")] string RiskCaveat);
