namespace KanelBrief.Core.Models;

/// <summary>Result of an Opportunity Scan agent run. Stored in the <c>OpportunityScanRuns</c> Azure Table.</summary>
public class OpportunityScanRun : AgentRunBase
{
    /// <summary>Discriminator constant — identifies this run shape on the wire and in YAML metadata.</summary>
    public string ReportType { get; set; } = "rotation-targets";

    public string SubstitutionChainRunId { get; set; } = string.Empty;

    /// <summary>
    /// Period this opportunity scan covers. Not stored in Azure Tables — lazy-filled on read
    /// by walking <see cref="SubstitutionChainRunId"/> → its <c>WeeklySummaryRunId</c>
    /// → that <see cref="WeeklySummaryRun"/>'s period.
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <inheritdoc cref="PeriodStart"/>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <inheritdoc cref="PeriodStart"/>
    public string PeriodIsoWeek { get; set; } = string.Empty;

    public List<RotationTarget> Targets { get; set; } = [];
}
