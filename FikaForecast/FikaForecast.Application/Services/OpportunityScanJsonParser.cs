using System.Text.Json;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Deterministic JSON parser that extracts structured rotation targets from
/// the Opportunity Scan Agent's JSON output.
/// </summary>
public class OpportunityScanJsonParser : IOpportunityScanParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger _logger;

    public OpportunityScanJsonParser(ILogger logger)
    {
        _logger = logger;
    }

    public OpportunityScanParseResult Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return new OpportunityScanParseResult([], false, ["Empty JSON input"]);

        var warnings = new List<string>();

        OpportunityScanOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<OpportunityScanOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize opportunity scan JSON output");
            return new OpportunityScanParseResult(
                [], false, [$"JSON deserialization failed: {ex.Message}"]);
        }

        if (output is null)
            return new OpportunityScanParseResult([], false, ["Deserialized output was null"]);

        var targets = new List<RotationTarget>();

        if (output.Targets is { Count: > 0 })
        {
            foreach (var t in output.Targets)
            {
                if (string.IsNullOrWhiteSpace(t.Category))
                {
                    warnings.Add("Skipping target with empty category");
                    continue;
                }

                if (!TryParseSignalStrength(t.SignalStrength, out var strength))
                {
                    warnings.Add($"Skipping target '{t.Category}' with invalid signal_strength '{t.SignalStrength}'");
                    continue;
                }

                targets.Add(new RotationTarget(
                    category: t.Category,
                    signalStrength: strength,
                    rationale: t.Rationale ?? string.Empty,
                    riskCaveat: t.RiskCaveat ?? string.Empty));
            }
        }
        else
        {
            // Empty targets array is valid — agent found no strong signals
            _logger.Debug("No targets in JSON output (agent found no strong signals)");
        }

        var isComplete = warnings.Count == 0;

        _logger.Debug("Parsed {0} rotation targets (complete: {1})", targets.Count, isComplete);

        if (warnings.Count > 0)
            _logger.Warn("Parse warnings: {0}", string.Join("; ", warnings));

        return new OpportunityScanParseResult(targets, isComplete, warnings);
    }

    private static bool TryParseSignalStrength(string? value, out SignalStrength result)
    {
        result = SignalStrength.Moderate;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Equals("strong", StringComparison.OrdinalIgnoreCase))
        {
            result = SignalStrength.Strong;
            return true;
        }

        if (value.Equals("moderate", StringComparison.OrdinalIgnoreCase))
        {
            result = SignalStrength.Moderate;
            return true;
        }

        return false;
    }
}
