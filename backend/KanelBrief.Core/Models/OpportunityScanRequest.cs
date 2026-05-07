namespace KanelBrief.Core.Models;

/// <summary>Input request for the Opportunity Scan agent.</summary>
public class OpportunityScanRequest
{
    /// <summary>RunDate (yyyy-MM-dd) of the substitution chain — the table's PartitionKey.</summary>
    public string SubstitutionChainRunDate { get; set; } = string.Empty;

    /// <summary>RunId (RowKey) of the substitution chain to base the opportunity analysis on.</summary>
    public SubstitutionChainRunId SubstitutionChainRunId { get; set; }
}
