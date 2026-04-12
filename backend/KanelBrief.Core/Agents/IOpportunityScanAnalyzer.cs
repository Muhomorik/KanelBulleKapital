using KanelBrief.Core.Models;

namespace KanelBrief.Core.Agents;

/// <summary>
/// Domain port for Opportunity Scan analysis. Takes rotation chains and returns investment targets.
/// </summary>
public interface IOpportunityScanAnalyzer
{
    Task<OpportunityScanAnalysisResult> AnalyzeAsync(
        SubstitutionChainRun substitutionChain,
        CancellationToken ct = default);
}
