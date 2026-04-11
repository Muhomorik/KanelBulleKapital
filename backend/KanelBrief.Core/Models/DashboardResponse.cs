namespace KanelBrief.Core.Models;

/// <summary>
/// Composite dashboard response combining all agent run types with pre-computed metadata.
/// Eliminates the need for clients to null-coalesce across individual run types.
/// </summary>
public class DashboardResponse
{
    /// <summary>Date of the most recent run found, or null if no data available.</summary>
    public string? RunDate { get; set; }

    /// <summary>True if at least one agent run was found.</summary>
    public bool HasData { get; set; }

    public NewsBriefRun? NewsBrief { get; set; }
    public WeeklySummaryRun? WeeklySummary { get; set; }
    public SubstitutionChainRun? SubstitutionChain { get; set; }
    public OpportunityScanRun? OpportunityScan { get; set; }
}
