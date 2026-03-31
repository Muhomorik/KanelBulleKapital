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
}
