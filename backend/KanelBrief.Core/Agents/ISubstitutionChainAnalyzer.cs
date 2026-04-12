using KanelBrief.Core.Models;

namespace KanelBrief.Core.Agents;

/// <summary>
/// Domain port for Substitution Chain analysis. Takes a weekly summary and returns capital rotation chains.
/// </summary>
public interface ISubstitutionChainAnalyzer
{
    Task<SubstitutionChainAnalysisResult> AnalyzeAsync(
        WeeklySummaryRun weeklySummary,
        CancellationToken ct = default);
}
