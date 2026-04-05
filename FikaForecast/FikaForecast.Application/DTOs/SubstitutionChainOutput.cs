using System.Text.Json.Serialization;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// JSON deserialization target for the Substitution Chain Agent's structured output.
/// </summary>
public sealed record SubstitutionChainOutput(
    List<SubstitutionChainOutputChain> Chains);

/// <summary>
/// A single rotation chain entry from the agent's JSON output.
/// </summary>
public sealed record SubstitutionChainOutputChain(
    [property: JsonPropertyName("capital_fleeing")] string CapitalFleeing,
    [property: JsonPropertyName("flows_toward")] string FlowsToward,
    string Mechanism);
