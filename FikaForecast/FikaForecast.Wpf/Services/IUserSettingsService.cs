namespace FikaForecast.Wpf.Services;

/// <summary>
/// Loads and saves user preferences from a local JSON file.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>Path to the settings file on disk.</summary>
    string SettingsFilePath { get; }

    UserSettings Load();
    void Save(UserSettings settings);
}
