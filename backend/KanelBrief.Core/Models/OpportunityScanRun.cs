namespace KanelBrief.Core.Models;

/// <summary>Result of an Opportunity Scan agent run. Stored in the <c>OpportunityScanRuns</c> Azure Table.</summary>
public class OpportunityScanRun : AgentRunBase
{
    public string SubstitutionChainRunId { get; set; } = string.Empty;

    public List<RotationTarget> Targets { get; set; } = [];
}
