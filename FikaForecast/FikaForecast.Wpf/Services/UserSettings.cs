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

    /// <summary>Folder where weekly report markdown files are written.</summary>
    public string? ExportFolderPath { get; set; }

    /// <summary>When true, each completed weekly agent run writes a markdown file to <see cref="ExportFolderPath"/>.</summary>
    public bool AutoExportEnabled { get; set; }

    /// <summary>Full path to the SQLite database file. Resolved at startup if null.</summary>
    public string? DatabasePath { get; set; }
}
