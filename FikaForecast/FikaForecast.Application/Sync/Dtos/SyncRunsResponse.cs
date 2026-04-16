namespace FikaForecast.Application.Sync.Dtos;

/// <summary>
/// Mirrors the backend <c>SyncRunsResponse&lt;T&gt;</c> envelope.
/// </summary>
public sealed class SyncRunsResponse<T>
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public int Count { get; set; }
    public List<T> Runs { get; set; } = [];
}
