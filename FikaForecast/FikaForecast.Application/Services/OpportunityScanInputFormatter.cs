using System.Text;
using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.Services;

/// <summary>
/// Formats a <see cref="SubstitutionChainRun"/> and its rotation chains into text input
/// for the Opportunity Scan Agent.
/// </summary>
public class OpportunityScanInputFormatter
{
    /// <summary>
    /// Formats the substitution chain run and its chains into a text block for the LLM prompt.
    /// The run must include eager-loaded Chains.
    /// </summary>
    public string Format(SubstitutionChainRun chainRun)
    {
        if (chainRun.Chains.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine("SUBSTITUTION CHAIN ANALYSIS");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Narrative rotation chains
        sb.AppendLine("Rotation chains:");
        foreach (var chain in chainRun.Chains)
        {
            sb.AppendLine($"- {chain.CapitalFleeing} → {chain.FlowsToward}: {chain.Mechanism}");
        }

        sb.AppendLine();

        // Structured substitution table
        sb.AppendLine("Substitution table:");
        sb.AppendLine();
        sb.AppendLine("| Capital fleeing | Flows toward | Mechanism |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var chain in chainRun.Chains)
        {
            sb.AppendLine($"| {chain.CapitalFleeing} | {chain.FlowsToward} | {chain.Mechanism} |");
        }

        return sb.ToString().TrimEnd();
    }
}
