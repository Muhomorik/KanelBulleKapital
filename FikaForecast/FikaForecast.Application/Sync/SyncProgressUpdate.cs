namespace FikaForecast.Application.Sync;

/// <summary>
/// Progress update for <see cref="IProgress{T}"/> binding during sync.
/// </summary>
public sealed record SyncProgressUpdate(
    string Stage,
    int Done,
    int Total,
    string Message);
