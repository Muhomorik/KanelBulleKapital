using KanelBrief.Core.Models;

namespace KanelBrief.Core.Repositories;

/// <summary>
/// Abstraction for persisting and retrieving agent run results.
/// Keeps the Core domain model free of Azure-specific concerns.
/// Implementation (with Azure Tables) lives in the Functions project.
/// </summary>
public interface IAgentRunRepository
{
    // News Brief Runs

    /// <summary>Persist a News Brief run result.</summary>
    Task SaveNewsBriefRunAsync(NewsBriefRun run);

    /// <summary>Retrieve a specific News Brief run by date and run ID.</summary>
    Task<NewsBriefRun?> GetNewsBriefRunAsync(string runDate, string runId);

    /// <summary>Retrieve all News Brief runs for a given date (partition scan).</summary>
    Task<List<NewsBriefRun>> GetNewsBriefRunsByDateAsync(string runDate);


    // Weekly Summary Runs

    /// <summary>Persist a Weekly Summary run result.</summary>
    Task SaveWeeklySummaryRunAsync(WeeklySummaryRun run);

    /// <summary>Retrieve a specific Weekly Summary run by date and run ID.</summary>
    Task<WeeklySummaryRun?> GetWeeklySummaryRunAsync(string runDate, string runId);

    /// <summary>Retrieve all Weekly Summary runs for a given date (partition scan).</summary>
    Task<List<WeeklySummaryRun>> GetWeeklySummaryRunsByDateAsync(string runDate);


    // Substitution Chain Runs

    /// <summary>Persist a Substitution Chain run result.</summary>
    Task SaveSubstitutionChainRunAsync(SubstitutionChainRun run);

    /// <summary>Retrieve a specific Substitution Chain run by date and run ID.</summary>
    Task<SubstitutionChainRun?> GetSubstitutionChainRunAsync(string runDate, string runId);

    /// <summary>Retrieve all Substitution Chain runs for a given date (partition scan).</summary>
    Task<List<SubstitutionChainRun>> GetSubstitutionChainRunsByDateAsync(string runDate);


    // Opportunity Scan Runs

    /// <summary>Persist an Opportunity Scan run result.</summary>
    Task SaveOpportunityScanRunAsync(OpportunityScanRun run);

    /// <summary>Retrieve a specific Opportunity Scan run by date and run ID.</summary>
    Task<OpportunityScanRun?> GetOpportunityScanRunAsync(string runDate, string runId);

    /// <summary>Retrieve all Opportunity Scan runs for a given date (partition scan).</summary>
    Task<List<OpportunityScanRun>> GetOpportunityScanRunsByDateAsync(string runDate);
}
