using FikaForecast.Application.Interfaces;

namespace FikaForecast.Wpf.Services;

/// <summary>
/// Reads export configuration from the on-disk <see cref="UserSettings"/> file every call
/// so the orchestrators see toggle changes immediately without an app restart.
/// </summary>
public class ExportSettingsProvider : IExportSettingsProvider
{
    private readonly IUserSettingsService _settings;

    public ExportSettingsProvider(IUserSettingsService settings)
    {
        _settings = settings;
    }

    public string? ExportFolderPath => _settings.Load().ExportFolderPath;

    public bool AutoExportEnabled => _settings.Load().AutoExportEnabled;
}
