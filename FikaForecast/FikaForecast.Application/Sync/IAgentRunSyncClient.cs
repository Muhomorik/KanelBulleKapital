using FikaForecast.Application.Sync.Dtos;

namespace FikaForecast.Application.Sync;

/// <summary>
/// HTTP abstraction for fetching agent runs from the backend sync endpoints.
/// </summary>
public interface IAgentRunSyncClient
{
    Task<List<SyncNewsBriefRun>> FetchNewsBriefRunsAsync(SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct);
    Task<List<SyncWeeklySummaryRun>> FetchWeeklySummaryRunsAsync(SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct);
    Task<List<SyncSubstitutionChainRun>> FetchSubstitutionChainRunsAsync(SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct);
    Task<List<SyncOpportunityScanRun>> FetchOpportunityScanRunsAsync(SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct);
}
