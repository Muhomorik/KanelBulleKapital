namespace KanelBrief.Core.Models;

/// <summary>Input request for the Substitution Chain agent.</summary>
public class SubstitutionChainRequest
{
    /// <summary>RunDate (yyyy-MM-dd) of the weekly summary — the table's PartitionKey.</summary>
    public string WeeklySummaryRunDate { get; set; } = string.Empty;

    /// <summary>RunId (RowKey) of the weekly summary to base the chain analysis on.</summary>
    public WeeklySummaryRunId WeeklySummaryRunId { get; set; }
}
