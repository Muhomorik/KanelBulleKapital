namespace FikaForecast.Application.Sync;

/// <summary>
/// Immutable outcome of a sync operation with per-type counters.
/// </summary>
public sealed record SyncResult(
    int Fetched,
    int Inserted,
    int Skipped,
    int Failed,
    long TotalDurationMs,
    string? ErrorMessage = null);
