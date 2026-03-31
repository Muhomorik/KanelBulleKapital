using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IUserSettingsService _settingsService;
    private readonly ILogger _logger;

    private ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    public ObservableCollection<ModelSettingItem> ModelItems { get; } = [];

    public ModelConfig? DefaultModel
    {
        get => GetValue<ModelConfig?>();
        set => SetValue(value);
    }

    /// <summary>Path shown in the UI so the user knows where settings are stored.</summary>
    public string SettingsFilePath { get; }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CloseCommand { get; }

    /// <summary>Runtime constructor (DI).</summary>
    public SettingsViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        IEnumerable<ModelConfig> allModels)
    {
        _logger = logger;
        _settingsService = settingsService;
        SettingsFilePath = settingsService.SettingsFilePath;

        var settings = settingsService.Load();

        // If no saved settings, enable all models by default.
        var hasSettings = settings.EnabledModelIds.Count > 0;

        foreach (var model in allModels)
        {
            var enabled = !hasSettings || settings.EnabledModelIds.Contains(model.ModelId);
            ModelItems.Add(new ModelSettingItem(model, enabled));
        }

        // Set default model from saved settings (or null).
        if (settings.DefaultModelId is not null)
        {
            DefaultModel = ModelItems
                .Where(m => m.IsEnabled)
                .Select(m => m.Model)
                .FirstOrDefault(m => m.ModelId == settings.DefaultModelId);
        }

        SaveCommand = new DelegateCommand(Save);
        CloseCommand = new DelegateCommand(() => CurrentWindowService?.Close());
    }

    /// <summary>Design-time constructor.</summary>
    public SettingsViewModel()
    {
        _settingsService = null!;
        _logger = null!;
        SettingsFilePath = @"%LocalAppData%\FikaForecast\settings.json";
        SaveCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
    }

    private void Save()
    {
        var settings = new UserSettings
        {
            EnabledModelIds = ModelItems
                .Where(m => m.IsEnabled)
                .Select(m => m.Model.ModelId)
                .ToList(),
            DefaultModelId = DefaultModel?.ModelId
        };

        _settingsService.Save(settings);
        _logger.Info("Settings saved");

        CurrentWindowService?.Close();
    }
}
