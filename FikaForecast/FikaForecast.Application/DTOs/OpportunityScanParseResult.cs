using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Result of parsing the Opportunity Scan Agent's JSON output into domain objects.
/// Contains rotation targets, completeness flag, and any warnings.
/// </summary>
public sealed record OpportunityScanParseResult(
    IReadOnlyList<RotationTarget> Targets,
    bool IsComplete,
    IReadOnlyList<string> Warnings);
