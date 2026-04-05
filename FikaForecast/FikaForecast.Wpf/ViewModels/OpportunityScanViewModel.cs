using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;
using FikaForecast.Wpf.Views;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Opportunity Scan (Targets) tab. Loads the latest substitution chain run,
/// runs the Opportunity Scan Agent, and displays past scan runs.
/// </summary>
/// <remarks>
/// Must be created on the UI thread. Async commands dispatch back to the UI thread automatically.
/// </remarks>
public class OpportunityScanViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISubstitutionChainRunRepository _chainRepository;
    private readonly IOpportunityScanRunRepository _scanRepository;
    private readonly OpportunityScanOrchestrator _orchestrator;
    private readonly OpportunityScanMarkdownRenderer _renderer;
    private readonly IPromptProvider _promptProvider;
    private readonly string? _defaultModelId;
    private readonly ModelConfig? _defaultModel;

    public ObservableCollection<OpportunityScanRun> Runs { get; } = [];

    /// <summary>
    /// Bound TwoWay to the tab's <c>IsSelected</c>.
    /// Reloads runs from the database each time the tab becomes active.
    /// </summary>
    public bool IsActive
    {
        get => GetValue<bool>();
        set => SetValue(value, changedCallback: OnIsActiveChanged);
    }

    public OpportunityScanRun? SelectedRun
    {
        get => GetValue<OpportunityScanRun?>();
        set => SetValue(value);
    }

    public bool IsRunning
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public string? StatusMessage
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    public bool IsStatusError
    {
        get => GetValue<bool>();
        private set => SetValue(value);
    }

    /// <summary>Display name of the default model used for the pipeline.</summary>
    public string? DefaultModelName
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    /// <summary>Info about the latest available substitution chain run (input data preview).</summary>
    public string? LatestChainInfo
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    /// <summary>Number of chains in the latest substitution chain run.</summary>
    public int LatestChainCount
    {
        get => GetValue<int>();
        private set => SetValue(value);
    }

    public ICommand RunScanCommand { get; }
    public ICommand LoadHistoryCommand { get; }
    public ICommand DeleteRunCommand { get; }
    public ICommand RegenerateMarkdownCommand { get; }

    public OpportunityScanViewModel(
        ILogger logger,
        ISubstitutionChainRunRepository chainRepository,
        IOpportunityScanRunRepository scanRepository,
        OpportunityScanOrchestrator orchestrator,
        OpportunityScanMarkdownRenderer renderer,
        IEnumerable<ModelConfig> models,
        IUserSettingsService settingsService,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _chainRepository = chainRepository;
        _scanRepository = scanRepository;
        _orchestrator = orchestrator;
        _renderer = renderer;
        _promptProvider = promptProvider;

        var settings = settingsService.Load();
        _defaultModelId = settings.DefaultModelId;

        var modelList = models.ToList();
        _defaultModel = _defaultModelId is not null
            ? modelList.FirstOrDefault(m => m.ModelId == _defaultModelId)
            : modelList.FirstOrDefault();

        DefaultModelName = _defaultModel?.DisplayName;

        RunScanCommand = new AsyncCommand(RunScanAsync, () => !IsRunning && _defaultModel is not null);
        LoadHistoryCommand = new AsyncCommand(LoadHistoryAsync);
        DeleteRunCommand = new AsyncCommand<OpportunityScanRun>(DeleteRunAsync);
        RegenerateMarkdownCommand = new AsyncCommand<OpportunityScanRun>(RegenerateMarkdownAsync);
    }

    private async Task RunScanAsync()
    {
        if (_defaultModel is null)
        {
            SetStatus("No default model configured. Set one in Settings.", isError: true);
            return;
        }

        IsRunning = true;
        SetStatus("Loading latest substitution chain run...");

        try
        {
            // Find the latest successful substitution chain run
            var allChains = await _chainRepository.GetAllAsync();
            var latestChain = allChains
                .FirstOrDefault(r => r.Status == RunStatus.Success && r.Chains.Count > 0);

            if (latestChain is null)
            {
                SetStatus("No successful substitution chain run found. Run Step 3 first.", isError: true);
                return;
            }

            SetStatus($"Running Opportunity Scan Agent with {latestChain.Chains.Count} chains...");

            var result = await _orchestrator.RunAsync(
                _defaultModel,
                _promptProvider.GetOpportunityScanPrompt(),
                latestChain);

            if (result.IsSuccess)
            {
                var run = result.Value;
                Runs.Insert(0, run);
                SelectedRun = run;
                SetStatus($"Complete — {run.Targets.Count} targets, {run.TotalTokens} tokens, {run.Duration.TotalSeconds:F1}s");
            }
            else
            {
                SetStatus($"Failed: {result.Errors.First().Message}", isError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Opportunity Scan run failed unexpectedly");
            SetStatus($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var runs = await _scanRepository.GetAllAsync();
            Runs.Clear();
            foreach (var run in runs)
                Runs.Add(run);

            SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load opportunity scan history");
        }
    }

    private async Task DeleteRunAsync(OpportunityScanRun? run)
    {
        if (run is null) return;

        var dialog = new ConfirmationDialog(
            $"Delete opportunity scan run from {run.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm}? This cannot be undone.");
        var confirmed = dialog.ShowDialog() ?? false;

        if (!confirmed) return;

        try
        {
            await _scanRepository.DeleteAsync(run.RunId);
            Runs.Remove(run);
            if (SelectedRun == run)
                SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete opportunity scan run");
        }
    }

    private async Task RegenerateMarkdownAsync(OpportunityScanRun? run)
    {
        if (run is null || run.Targets.Count == 0) return;

        try
        {
            // Re-render from existing structured data (no LLM call)
            var parseResult = new OpportunityScanParseResult(run.Targets, true, []);
            var markdown = _renderer.Render(parseResult);

            run.SetDisplayMarkdown(markdown);
            await _scanRepository.UpdateAsync(run);

            // Force WebView2 refresh by re-selecting
            SelectedRun = null;
            SelectedRun = run;

            SetStatus($"Markdown regenerated for {run.Targets.Count} targets");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to regenerate markdown");
            SetStatus($"Failed to regenerate: {ex.Message}", isError: true);
        }
    }

    private void OnIsActiveChanged()
    {
        if (IsActive)
        {
            ((AsyncCommand)LoadHistoryCommand).Execute(null);
            _ = LoadLatestChainInfoAsync();
        }
    }

    /// <summary>
    /// Loads info about the latest successful substitution chain run to show in the input data preview.
    /// </summary>
    private async Task LoadLatestChainInfoAsync()
    {
        try
        {
            var allChains = await _chainRepository.GetAllAsync();
            var latest = allChains
                .FirstOrDefault(r => r.Status == RunStatus.Success && r.Chains.Count > 0);

            if (latest is null)
            {
                LatestChainInfo = null;
                LatestChainCount = 0;
                return;
            }

            LatestChainInfo = $"{latest.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm}";
            LatestChainCount = latest.Chains.Count;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load latest chain info");
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
