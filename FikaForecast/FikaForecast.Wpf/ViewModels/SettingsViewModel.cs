using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Application.Sync;
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
    private readonly ISyncService _syncService;
    private readonly IFilePicker _filePicker;
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

    /// <summary>Full path to the SQLite database file. Edits take effect after restart.</summary>
    public string? DatabasePath
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    #region Sync properties

    public string? SyncBaseUrl
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public string? SyncAuthToken
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public SyncRange SyncRange
    {
        get => GetValue<SyncRange>();
        set => SetValue(value);
    }

    public bool IsSyncing
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public string? SyncStatusText
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public int SyncProgressCurrent
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public int SyncProgressTotal
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    #endregion

    #region Commands

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DelegateCommand ResetPromptCommand { get; }
    public DelegateCommand BrowseDatabasePathCommand { get; }
    public AsyncCommand SyncCommand { get; }

    #endregion

    /// <summary>Runtime constructor (DI).</summary>
    public SettingsViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        IPromptFileService promptFileService,
        IPromptProvider promptProvider,
        ISyncService syncService,
        IFilePicker filePicker,
        IEnumerable<ModelConfig> allModels)
    {
        _logger = logger;
        _settingsService = settingsService;
        _promptFileService = promptFileService;
        _promptProvider = promptProvider;
        _syncService = syncService;
        _filePicker = filePicker;
        SettingsFilePath = settingsService.SettingsFilePath;

        var currentSettings = settingsService.Load();
        DatabasePath = currentSettings.DatabasePath;
        LoadModelSettings(currentSettings, allModels);
        LoadPrompts();
        LoadSyncSettings(currentSettings);

        SaveCommand = new DelegateCommand(Save);
        CloseCommand = new DelegateCommand(() => CurrentWindowService?.Close());
        ResetPromptCommand = new DelegateCommand(ResetPrompt, () => SelectedPrompt is not null);
        BrowseDatabasePathCommand = new DelegateCommand(BrowseDatabasePath);
        SyncCommand = new AsyncCommand(RunSyncAsync, () => !IsSyncing);
    }

    /// <summary>Design-time constructor.</summary>
    public SettingsViewModel()
    {
        _settingsService = null!;
        _promptFileService = null!;
        _promptProvider = null!;
        _syncService = null!;
        _filePicker = null!;
        _logger = null!;
        SettingsFilePath = @"%LocalAppData%\FikaForecast\settings.json";
        DatabasePath = @"%UserProfile%\Documents\fikaforecast.db";

        PromptItems.Add(new PromptSettingItem("newsbrief", "News Brief - Default", "Sample prompt body..."));
        SelectedPrompt = PromptItems.FirstOrDefault();
        SaveCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        ResetPromptCommand = new DelegateCommand(() => { });
        BrowseDatabasePathCommand = new DelegateCommand(() => { });
        SyncCommand = new AsyncCommand(() => Task.CompletedTask);
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

    private void LoadSyncSettings(UserSettings settings)
    {
        SyncBaseUrl = settings.SyncBaseUrl;
        SyncAuthToken = settings.SyncAuthToken;
        SyncRange = SyncRange.OneDay;
    }

    private void Save()
    {
        // Load-merge-save: preserve fields we don't manage on this tab (e.g. sync fields).
        var settings = _settingsService.Load();

        settings.EnabledModelIds = ModelItems
            .Where(m => m.IsEnabled)
            .Select(m => m.Model.ModelId)
            .ToList();
        settings.DefaultModelId = DefaultModel?.ModelId;
        settings.SyncBaseUrl = SyncBaseUrl;
        settings.SyncAuthToken = SyncAuthToken;
        settings.DatabasePath = string.IsNullOrWhiteSpace(DatabasePath) ? null : DatabasePath;

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

    private async Task RunSyncAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncBaseUrl) || string.IsNullOrWhiteSpace(SyncAuthToken))
        {
            SyncStatusText = "Enter a sync URL and token first.";
            return;
        }

        // Persist sync settings before running so they survive if the app crashes.
        var settings = _settingsService.Load();
        settings.SyncBaseUrl = SyncBaseUrl;
        settings.SyncAuthToken = SyncAuthToken;
        _settingsService.Save(settings);

        IsSyncing = true;
        SyncStatusText = "Syncing...";
        SyncProgressCurrent = 0;
        SyncProgressTotal = 0;

        var connection = new SyncConnectionInfo(SyncBaseUrl!, SyncAuthToken!);
        var progress = new Progress<SyncProgressUpdate>(update =>
        {
            SyncStatusText = $"{update.Stage}: {update.Message}";
            SyncProgressCurrent = update.Done;
            SyncProgressTotal = update.Total;
        });

        try
        {
            var result = await _syncService.RunAsync(connection, SyncRange, progress, CancellationToken.None);

            SyncStatusText = result.ErrorMessage
                ?? $"Done — {result.Inserted} inserted, {result.Skipped} skipped, {result.Failed} failed ({result.TotalDurationMs}ms)";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Sync failed unexpectedly");
            SyncStatusText = $"Sync failed: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void BrowseDatabasePath()
    {
        var chosen = _filePicker.Pick(
            DatabasePath,
            "Choose database file",
            "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            "fikaforecast.db");
        if (!string.IsNullOrWhiteSpace(chosen))
            DatabasePath = chosen;
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
