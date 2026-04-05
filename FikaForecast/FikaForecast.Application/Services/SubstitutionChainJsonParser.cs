using System.Text.Json;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Deterministic JSON parser that extracts structured rotation chains from
/// the Substitution Chain Agent's JSON output.
/// </summary>
public class SubstitutionChainJsonParser : ISubstitutionChainParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger _logger;

    public SubstitutionChainJsonParser(ILogger logger)
    {
        _logger = logger;
    }

    public SubstitutionChainParseResult Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return new SubstitutionChainParseResult([], false, ["Empty JSON input"]);

        var warnings = new List<string>();

        SubstitutionChainOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<SubstitutionChainOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize substitution chain JSON output");
            return new SubstitutionChainParseResult(
                [], false, [$"JSON deserialization failed: {ex.Message}"]);
        }

        if (output is null)
            return new SubstitutionChainParseResult([], false, ["Deserialized output was null"]);

        var chains = new List<RotationChain>();

        if (output.Chains is { Count: > 0 })
        {
            foreach (var c in output.Chains)
            {
                if (string.IsNullOrWhiteSpace(c.CapitalFleeing))
                {
                    warnings.Add("Skipping chain with empty capital_fleeing");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(c.FlowsToward))
                {
                    warnings.Add($"Skipping chain '{c.CapitalFleeing}' with empty flows_toward");
                    continue;
                }

                chains.Add(new RotationChain(
                    capitalFleeing: c.CapitalFleeing,
                    flowsToward: c.FlowsToward,
                    mechanism: c.Mechanism ?? string.Empty));
            }
        }
        else
        {
            warnings.Add("No chains found in JSON output");
        }

        var isComplete = warnings.Count == 0 && chains.Count > 0;

        _logger.Debug("Parsed {0} rotation chains (complete: {1})", chains.Count, isComplete);

        if (warnings.Count > 0)
            _logger.Warn("Parse warnings: {0}", string.Join("; ", warnings));

        return new SubstitutionChainParseResult(chains, isComplete, warnings);
    }
}
