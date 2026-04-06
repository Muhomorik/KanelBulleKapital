namespace KanelBrief.Core.Models;

/// <summary>Result of a Substitution Chain agent run. Stored in the <c>SubstitutionChainRuns</c> Azure Table.</summary>
public class SubstitutionChainRun : AgentRunBase
{
    public string WeeklySummaryRunId { get; set; } = string.Empty;

    public List<RotationChain> Chains { get; set; } = [];
}
