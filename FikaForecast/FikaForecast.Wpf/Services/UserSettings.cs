namespace FikaForecast.Wpf.Services;

/// <summary>
/// User preferences persisted to a local JSON file.
/// </summary>
public class UserSettings
{
    /// <summary>Model IDs the user has enabled for the Comparison view.</summary>
    public List<string> EnabledModelIds { get; set; } = [];

    /// <summary>Model ID pre-selected in the Comparison view at startup.</summary>
    public string? DefaultModelId { get; set; }

    /// <summary>Base URL of the backend sync endpoints (e.g. <c>https://myapp.azurewebsites.net</c>).</summary>
    public string? SyncBaseUrl { get; set; }

    /// <summary>Bearer token for authenticating sync requests. Stored plaintext (single-user hobby use).</summary>
    public string? SyncAuthToken { get; set; }
}
