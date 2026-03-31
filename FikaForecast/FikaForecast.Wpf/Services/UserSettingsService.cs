using System.IO;
using System.Text.Json;
using NLog;

namespace FikaForecast.Wpf.Services;

/// <summary>
/// Persists <see cref="UserSettings"/> to a JSON file in <c>%LocalAppData%\FikaForecast\</c>.
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger _logger;

    public string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FikaForecast", "settings.json");

    public UserSettingsService(ILogger logger)
    {
        _logger = logger;
    }

    public UserSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            _logger.Debug("No settings file found at {0}, using defaults", SettingsFilePath);
            return new UserSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Failed to read settings from {0}, using defaults", SettingsFilePath);
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsFilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);

        _logger.Info("Settings saved to {0}", SettingsFilePath);
    }
}
