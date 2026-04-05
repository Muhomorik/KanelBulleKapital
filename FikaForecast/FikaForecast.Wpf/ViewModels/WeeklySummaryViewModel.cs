using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
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
/// ViewModel for the Weekly Summary tab. Loads recent daily briefs from the default model,
/// runs the Weekly Summary Agent, and displays past summaries.
/// </summary>
/// <remarks>
/// Must be created on the UI thread. Async commands dispatch back to the UI thread automatically.
/// </remarks>
public class WeeklySummaryViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly INewsBriefRunRepository _newsBriefRepository;
    private readonly IWeeklySummaryRunRepository _summaryRepository;
    private readonly WeeklySummaryOrchestrator _orchestrator;
    private readonly IPromptProvider _promptProvider;
    private readonly string? _defaultModelId;
    private readonly ModelConfig? _defaultModel;

    public ObservableCollection<WeeklySummaryRun> Runs { get; } = [];
    public ObservableCollection<DailyBriefInfo> AvailableBriefs { get; } = [];

    /// <summary>
    /// Bound TwoWay to the tab's <c>IsSelected</c>.
    /// Reloads runs from the database each time the tab becomes active.
    /// </summary>
    public bool IsActive
    {
        get => GetValue<bool>();
        set => SetValue(value, changedCallback: OnIsActiveChanged);
    }

    public WeeklySummaryRun? SelectedRun
    {
        get => GetValue<WeeklySummaryRun?>();
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

    /// <summary>Number of daily briefs available for the default model.</summary>
    public int DailyBriefCount
    {
        get => GetValue<int>();
        private set => SetValue(value);
    }

    /// <summary>Date range summary of available briefs, e.g. "Apr 01 – Apr 05, 2026".</summary>
    public string? BriefsDateRange
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    /// <summary>Display name of the default model used for the pipeline.</summary>
    public string? DefaultModelName
    {
        get => GetValue<string?>();
        private set => SetValue(value);
    }

    public ICommand RunSummaryCommand { get; }
    public ICommand LoadHistoryCommand { get; }
    public ICommand DeleteRunCommand { get; }

    public WeeklySummaryViewModel(
        ILogger logger,
        INewsBriefRunRepository newsBriefRepository,
        IWeeklySummaryRunRepository summaryRepository,
        WeeklySummaryOrchestrator orchestrator,
        IEnumerable<ModelConfig> models,
        IUserSettingsService settingsService,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _newsBriefRepository = newsBriefRepository;
        _summaryRepository = summaryRepository;
        _orchestrator = orchestrator;
        _promptProvider = promptProvider;

        var settings = settingsService.Load();
        _defaultModelId = settings.DefaultModelId;

        var modelList = models.ToList();
        _defaultModel = _defaultModelId is not null
            ? modelList.FirstOrDefault(m => m.ModelId == _defaultModelId)
            : modelList.FirstOrDefault();

        DefaultModelName = _defaultModel?.DisplayName;

        RunSummaryCommand = new AsyncCommand(RunSummaryAsync, () => !IsRunning && _defaultModel is not null);
        LoadHistoryCommand = new AsyncCommand(LoadHistoryAsync);
        DeleteRunCommand = new AsyncCommand<WeeklySummaryRun>(DeleteRunAsync);
    }

    private async Task RunSummaryAsync()
    {
        if (_defaultModel is null)
        {
            SetStatus("No default model configured. Set one in Settings.", isError: true);
            return;
        }

        IsRunning = true;
        SetStatus($"Loading daily briefs for {_defaultModel.DisplayName}...");

        try
        {
            // Load recent daily briefs from the default model
            var allRuns = await _newsBriefRepository.GetByModelAsync(_defaultModelId!);
            var successfulRuns = allRuns
                .Where(r => r.Status == RunStatus.Success && r.Item is not null)
                .ToList();

            if (successfulRuns.Count == 0)
            {
                SetStatus("No successful daily briefs found for the default model. Run Step 1 first.", isError: true);
                return;
            }

            DailyBriefCount = successfulRuns.Count;
            SetStatus($"Running Weekly Summary Agent with {successfulRuns.Count} daily briefs...");

            var result = await _orchestrator.RunSummaryAsync(
                _defaultModel,
                _promptProvider.GetWeeklySummaryPrompt(),
                successfulRuns);

            if (result.IsSuccess)
            {
                var run = result.Value;
                Runs.Insert(0, run);
                SelectedRun = run;
                SetStatus($"Complete — {run.Themes.Count} themes, {run.TotalTokens} tokens, {run.Duration.TotalSeconds:F1}s");
            }
            else
            {
                SetStatus($"Failed: {result.Errors.First().Message}", isError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Weekly Summary run failed unexpectedly");
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
            var runs = await _summaryRepository.GetAllAsync();
            Runs.Clear();
            foreach (var run in runs)
                Runs.Add(run);

            SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load weekly summary history");
        }
    }

    private async Task DeleteRunAsync(WeeklySummaryRun? run)
    {
        if (run is null) return;

        var dialog = new ConfirmationDialog(
            $"Delete weekly summary from {run.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm}? This cannot be undone.");
        var confirmed = dialog.ShowDialog() ?? false;

        if (!confirmed) return;

        try
        {
            await _summaryRepository.DeleteAsync(run.RunId);
            Runs.Remove(run);
            if (SelectedRun == run)
                SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete weekly summary run");
        }
    }

    private void OnIsActiveChanged()
    {
        if (IsActive)
        {
            ((AsyncCommand)LoadHistoryCommand).Execute(null);
            _ = LoadAvailableBriefsAsync();
        }
    }

    /// <summary>
    /// Loads available Step 1 daily briefs for the default model and populates
    /// the input data preview (count, date range, per-day breakdown).
    /// </summary>
    private async Task LoadAvailableBriefsAsync()
    {
        if (_defaultModelId is null) return;

        try
        {
            var allRuns = await _newsBriefRepository.GetByModelAsync(_defaultModelId);
            var successfulRuns = allRuns
                .Where(r => r.Status == RunStatus.Success && r.Item is not null)
                .OrderBy(r => r.Timestamp)
                .ToList();

            DailyBriefCount = successfulRuns.Count;
            AvailableBriefs.Clear();

            if (successfulRuns.Count == 0)
            {
                BriefsDateRange = null;
                return;
            }

            var first = successfulRuns.First().Timestamp;
            var last = successfulRuns.Last().Timestamp;
            BriefsDateRange = $"{first:MMM dd} – {last:MMM dd, yyyy}";

            // Group by calendar date, show per-day breakdown
            var byDate = successfulRuns
                .GroupBy(r => r.Timestamp.Date)
                .OrderBy(g => g.Key);

            foreach (var dayGroup in byDate)
            {
                var latestRun = dayGroup.OrderByDescending(r => r.Timestamp).First();
                var categoryCount = latestRun.Item?.Assessments.Count ?? 0;
                var mood = latestRun.Item?.Mood ?? MarketSentiment.Mixed;

                AvailableBriefs.Add(new DailyBriefInfo(
                    Date: dayGroup.Key,
                    Mood: mood,
                    CategoryCount: categoryCount));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load available briefs");
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}

/// <summary>
/// Summary info for a single day's brief, displayed in the input data preview.
/// </summary>
public sealed record DailyBriefInfo(DateTimeOffset Date, MarketSentiment Mood, int CategoryCount)
{
    public string MoodEmoji => Mood switch
    {
        MarketSentiment.RiskOff => "🔴",
        MarketSentiment.RiskOn => "🟢",
        _ => "🟡"
    };
}
