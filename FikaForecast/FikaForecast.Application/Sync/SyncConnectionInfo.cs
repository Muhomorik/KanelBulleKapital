namespace FikaForecast.Application.Sync;

/// <summary>
/// Connection details for the backend sync endpoints.
/// Passed explicitly to avoid Infrastructure → WPF dependency.
/// </summary>
public sealed record SyncConnectionInfo(string BaseUrl, string AuthToken);
