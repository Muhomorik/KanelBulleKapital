namespace KanelBrief.Core.Models;

/// <summary>Result of a Substitution Chain agent run. Stored in the <c>SubstitutionChainRuns</c> Azure Table.</summary>
public class SubstitutionChainRun : AgentRunBase
{
    /// <summary>Discriminator constant — identifies this run shape on the wire and in YAML metadata.</summary>
    public string ReportType { get; set; } = "substitution-chain";

    public string WeeklySummaryRunId { get; set; } = string.Empty;

    /// <summary>
    /// Period this chain analysis covers. Not stored in Azure Tables — lazy-filled on read
    /// from the parent <see cref="WeeklySummaryRun"/> (via <see cref="WeeklySummaryRunId"/>).
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <inheritdoc cref="PeriodStart"/>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <inheritdoc cref="PeriodStart"/>
    public string PeriodIsoWeek { get; set; } = string.Empty;

    public List<RotationChain> Chains { get; set; } = [];
}
