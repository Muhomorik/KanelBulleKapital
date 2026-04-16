namespace FikaForecast.Application.Sync;

/// <summary>
/// Orchestrates pulling agent runs from the backend and inserting into local SQLite.
/// </summary>
public interface ISyncService
{
    Task<SyncResult> RunAsync(
        SyncConnectionInfo connection,
        SyncRange range,
        IProgress<SyncProgressUpdate>? progress,
        CancellationToken ct);
}
