using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private static readonly string[] PromptKeys = ["newsbrief", "evaluation", "comparison", "weeklysummary", "substitutionchain", "opportunityscan"];

    private readonly IUserSettingsService _settingsService;
    private readonly IPromptFileService _promptFileService;
    private readonly IPromptProvider _promptProvider;
    private readonly ILogger _logger;

    private ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    public ObservableCollection<ModelSettingItem> ModelItems { get; } = [];
    public ObservableCollection<PromptSettingItem> PromptItems { get; } = [];

    public ModelConfig? DefaultModel
    {
        get => GetValue<ModelConfig?>();
        set => SetValue(value);
    }

    public PromptSettingItem? SelectedPrompt
    {
        get => GetValue<PromptSettingItem?>();
        set => SetValue(value);
    }

    public int SelectedTabIndex
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    /// <summary>Path shown in the UI so the user knows where settings are stored.</summary>
    public string SettingsFilePath { get; }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DelegateCommand ResetPromptCommand { get; }

    /// <summary>Runtime constructor (DI).</summary>
    public SettingsViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        IPromptFileService promptFileService,
        IPromptProvider promptProvider,
        IEnumerable<ModelConfig> allModels)
    {
        _logger = logger;
        _settingsService = settingsService;
        _promptFileService = promptFileService;
        _promptProvider = promptProvider;
        SettingsFilePath = settingsService.SettingsFilePath;

        LoadModelSettings(settingsService.Load(), allModels);
        LoadPrompts();

        SaveCommand = new DelegateCommand(Save);
        CloseCommand = new DelegateCommand(() => CurrentWindowService?.Close());
        ResetPromptCommand = new DelegateCommand(ResetPrompt, () => SelectedPrompt is not null);
    }

    /// <summary>Design-time constructor.</summary>
    public SettingsViewModel()
    {
        _settingsService = null!;
        _promptFileService = null!;
        _promptProvider = null!;
        _logger = null!;
        SettingsFilePath = @"%LocalAppData%\FikaForecast\settings.json";

        PromptItems.Add(new PromptSettingItem("newsbrief", "News Brief - Default", "Sample prompt body..."));
        SelectedPrompt = PromptItems.FirstOrDefault();
        SaveCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        ResetPromptCommand = new DelegateCommand(() => { });
    }

    private void LoadModelSettings(UserSettings settings, IEnumerable<ModelConfig> allModels)
    {
        var hasSettings = settings.EnabledModelIds.Count > 0;

        foreach (var model in allModels)
        {
            var enabled = !hasSettings || settings.EnabledModelIds.Contains(model.ModelId);
            ModelItems.Add(new ModelSettingItem(model, enabled));
        }

        if (settings.DefaultModelId is not null)
        {
            DefaultModel = ModelItems
                .Where(m => m.IsEnabled)
                .Select(m => m.Model)
                .FirstOrDefault(m => m.ModelId == settings.DefaultModelId);
        }
    }

    private void LoadPrompts()
    {
        foreach (var key in PromptKeys)
        {
            var content = _promptFileService.ReadPromptFile(key);
            var (name, body) = PromptFileParser.Parse(content, key);
            PromptItems.Add(new PromptSettingItem(key, name, body));
        }

        SelectedPrompt = PromptItems.FirstOrDefault();
    }

    private void Save()
    {
        // Save model settings
        var settings = new UserSettings
        {
            EnabledModelIds = ModelItems
                .Where(m => m.IsEnabled)
                .Select(m => m.Model.ModelId)
                .ToList(),
            DefaultModelId = DefaultModel?.ModelId
        };
        _settingsService.Save(settings);

        // Save dirty prompts
        var dirtyPrompts = PromptItems.Where(p => p.IsDirty).ToList();
        foreach (var prompt in dirtyPrompts)
        {
            var fileContent = $"---\nName: {prompt.DisplayName}\n---\n{prompt.Body}\n";
            _promptFileService.WritePromptFile(prompt.PromptKey, fileContent);
            prompt.ResetOriginal(prompt.Body);
        }

        if (dirtyPrompts.Count > 0)
        {
            _promptProvider.InvalidateCache();
            _logger.Info("Saved {0} prompt(s) and invalidated cache", dirtyPrompts.Count);
        }

        _logger.Info("Settings saved");
        CurrentWindowService?.Close();
    }

    private void ResetPrompt()
    {
        if (SelectedPrompt is null) return;

        _promptFileService.ResetToDefault(SelectedPrompt.PromptKey);

        var content = _promptFileService.ReadPromptFile(SelectedPrompt.PromptKey);
        var (_, body) = PromptFileParser.Parse(content, SelectedPrompt.PromptKey);

        SelectedPrompt.Body = body;
        SelectedPrompt.ResetOriginal(body);

        _logger.Info("Prompt '{0}' reset to default", SelectedPrompt.PromptKey);
    }
}
