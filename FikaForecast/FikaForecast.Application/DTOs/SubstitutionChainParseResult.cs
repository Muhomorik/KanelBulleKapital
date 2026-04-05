using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Result of parsing the Substitution Chain Agent's JSON output into domain objects.
/// Contains rotation chains, completeness flag, and any warnings.
/// </summary>
public sealed record SubstitutionChainParseResult(
    IReadOnlyList<RotationChain> Chains,
    bool IsComplete,
    IReadOnlyList<string> Warnings);
