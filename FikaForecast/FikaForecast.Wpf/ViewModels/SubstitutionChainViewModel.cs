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
/// ViewModel for the Substitution Chain tab. Loads the latest weekly summary,
/// runs the Substitution Chain Agent, and displays past chain runs.
/// </summary>
/// <remarks>
/// Must be created on the UI thread. Async commands dispatch back to the UI thread automatically.
/// </remarks>
public class SubstitutionChainViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IWeeklySummaryRunRepository _summaryRepository;
    private readonly ISubstitutionChainRunRepository _chainRepository;
    private readonly SubstitutionChainOrchestrator _orchestrator;
    private readonly SubstitutionChainMarkdownRenderer _renderer;
    private readonly IPromptProvider _promptProvider;
    private readonly string? _defaultModelId;
    private readonly ModelConfig? _defaultModel;

    public ObservableCollection<SubstitutionChainRun> Runs { get; } = [];

    /// <summary>
    /// Bound TwoWay to the tab's <c>IsSelected</c>.
    /// Reloads runs from the database each time the tab becomes active.
    /// </summary>
    public bool IsActive
    {
        get => GetValue<bool>();
        set => SetValue(value, changedCallback: OnIsActiveChanged);
    }

    public SubstitutionChainRun? SelectedRun
    {
        get => GetValue<SubstitutionChainRun?>();
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

    /// <summary>Info about the latest available weekly summary (input data preview).</summary>
    public string? LatestSummaryInfo
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    /// <summary>Number of themes in the latest weekly summary.</summary>
    public int LatestSummaryThemeCount
    {
        get => GetValue<int>();
        private set => SetValue(value);
    }

    /// <summary>Net mood of the latest weekly summary.</summary>
    public string? LatestSummaryMood
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    public ICommand RunChainCommand { get; }
    public ICommand LoadHistoryCommand { get; }
    public ICommand DeleteRunCommand { get; }
    public ICommand RegenerateMarkdownCommand { get; }

    public SubstitutionChainViewModel(
        ILogger logger,
        IWeeklySummaryRunRepository summaryRepository,
        ISubstitutionChainRunRepository chainRepository,
        SubstitutionChainOrchestrator orchestrator,
        SubstitutionChainMarkdownRenderer renderer,
        IEnumerable<ModelConfig> models,
        IUserSettingsService settingsService,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _summaryRepository = summaryRepository;
        _chainRepository = chainRepository;
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

        RunChainCommand = new AsyncCommand(RunChainAsync, () => !IsRunning && _defaultModel is not null);
        LoadHistoryCommand = new AsyncCommand(LoadHistoryAsync);
        DeleteRunCommand = new AsyncCommand<SubstitutionChainRun>(DeleteRunAsync);
        RegenerateMarkdownCommand = new AsyncCommand<SubstitutionChainRun>(RegenerateMarkdownAsync);
    }

    private async Task RunChainAsync()
    {
        if (_defaultModel is null)
        {
            SetStatus("No default model configured. Set one in Settings.", isError: true);
            return;
        }

        IsRunning = true;
        SetStatus("Loading latest weekly summary...");

        try
        {
            // Find the latest successful weekly summary
            var allSummaries = await _summaryRepository.GetAllAsync();
            var latestSummary = allSummaries
                .FirstOrDefault(r => r.Status == RunStatus.Success && r.Themes.Count > 0);

            if (latestSummary is null)
            {
                SetStatus("No successful weekly summary found. Run Step 2 first.", isError: true);
                return;
            }

            SetStatus($"Running Substitution Chain Agent with {latestSummary.Themes.Count} themes...");

            var result = await _orchestrator.RunAsync(
                _defaultModel,
                _promptProvider.GetSubstitutionChainPrompt(),
                latestSummary);

            if (result.IsSuccess)
            {
                var run = result.Value;
                Runs.Insert(0, run);
                SelectedRun = run;
                SetStatus($"Complete — {run.Chains.Count} chains, {run.TotalTokens} tokens, {run.Duration.TotalSeconds:F1}s");
            }
            else
            {
                SetStatus($"Failed: {result.Errors.First().Message}", isError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Substitution Chain run failed unexpectedly");
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
            var runs = await _chainRepository.GetAllAsync();
            Runs.Clear();
            foreach (var run in runs)
                Runs.Add(run);

            SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load substitution chain history");
        }
    }

    private async Task DeleteRunAsync(SubstitutionChainRun? run)
    {
        if (run is null) return;

        var dialog = new ConfirmationDialog(
            $"Delete substitution chain run from {run.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm}? This cannot be undone.");
        var confirmed = dialog.ShowDialog() ?? false;

        if (!confirmed) return;

        try
        {
            await _chainRepository.DeleteAsync(run.RunId);
            Runs.Remove(run);
            if (SelectedRun == run)
                SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete substitution chain run");
        }
    }

    private async Task RegenerateMarkdownAsync(SubstitutionChainRun? run)
    {
        if (run is null || run.Chains.Count == 0) return;

        try
        {
            // Load the linked weekly summary for date range
            var summary = await _summaryRepository.GetByIdAsync(run.WeeklySummaryRunId);
            var weekStart = summary?.WeekStart ?? run.Timestamp;
            var weekEnd = summary?.WeekEnd ?? run.Timestamp;

            // Re-render from existing structured data (no LLM call)
            var parseResult = new SubstitutionChainParseResult(run.Chains, true, []);
            var markdown = _renderer.Render(parseResult, weekStart, weekEnd);

            run.SetDisplayMarkdown(markdown);
            await _chainRepository.UpdateAsync(run);

            // Force WebView2 refresh by re-selecting
            SelectedRun = null;
            SelectedRun = run;

            SetStatus($"Markdown regenerated for {run.Chains.Count} chains");
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
            _ = LoadLatestSummaryInfoAsync();
        }
    }

    /// <summary>
    /// Loads info about the latest successful weekly summary to show in the input data preview.
    /// </summary>
    private async Task LoadLatestSummaryInfoAsync()
    {
        try
        {
            var allSummaries = await _summaryRepository.GetAllAsync();
            var latest = allSummaries
                .FirstOrDefault(r => r.Status == RunStatus.Success && r.Themes.Count > 0);

            if (latest is null)
            {
                LatestSummaryInfo = null;
                LatestSummaryThemeCount = 0;
                LatestSummaryMood = null;
                return;
            }

            LatestSummaryInfo = $"{latest.WeekStart:MMM dd} – {latest.WeekEnd:MMM dd, yyyy}";
            LatestSummaryThemeCount = latest.Themes.Count;

            var moodEmoji = latest.NetMood switch
            {
                MarketSentiment.RiskOff => "🔴",
                MarketSentiment.RiskOn => "🟢",
                _ => "🟡"
            };
            LatestSummaryMood = $"{moodEmoji} {latest.MoodSummary}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load latest summary info");
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
