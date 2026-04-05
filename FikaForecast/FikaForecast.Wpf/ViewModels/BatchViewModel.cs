using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Windows.Input;

using DevExpress.Mvvm;

using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;

using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Batch Scheduler tab.
/// Manages Rx.NET timers for daily time slots and delegates schedule building
/// and batch execution to <see cref="IBatchSchedulingService"/>.
/// Supports auto-start via CLI <c>--auto-schedule</c> and manual toggle from the UI.
/// </summary>
public class BatchViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(4);
    private static readonly DayOfWeek WeeklySummaryDay = DayOfWeek.Thursday;
    private static readonly TimeOnly WeeklySummaryTime = new(22, 0);

    private readonly ILogger _logger;
    private readonly IBatchSchedulingService _schedulingService;
    private readonly IPromptProvider _promptProvider;
    private readonly IReadOnlyList<ModelConfig> _models;
    private readonly WeeklySummaryOrchestrator _orchestrator;
    private readonly INewsBriefRunRepository _newsBriefRepository;
    private readonly IUserSettingsService _settingsService;
    private readonly ModelConfig? _defaultModel;

    private readonly CompositeDisposable _disposables = new();
    private readonly SerialDisposable _slotTimer = new();
    private readonly SerialDisposable _weeklyTimer = new();
    private CancellationTokenSource? _batchCts;
    private CancellationTokenSource? _weeklyCts;

    /// <summary>All planned time slots for today.</summary>
    public ObservableCollection<ScheduledSlot> TodaySlots { get; } = [];

    /// <summary>True when the recurring scheduler is active.</summary>
    public bool IsSchedulerActive
    {
        get => GetValue<bool>();
        private set => SetValue(value);
    }

    /// <summary>True while a batch run is executing.</summary>
    public bool IsBatchRunning
    {
        get => GetValue<bool>();
        private set => SetValue(value);
    }

    /// <summary>Status message shown below the controls.</summary>
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

    /// <summary>True when launched with <c>--auto-schedule</c>. Bound OneTime to select the Batch tab on startup.</summary>
    public bool IsAutoSchedule { get; }

    /// <summary>Today's date for the schedule header.</summary>
    public string TodayDate
    {
        get => GetValue<string>();
        private set => SetValue(value);
    }

    /// <summary>Weekly summary schedule info displayed in a separate UI section.</summary>
    public WeeklySummaryScheduleInfo WeeklySummaryInfo { get; }

    public ICommand ToggleSchedulerCommand { get; }
    public ICommand RunWeeklySummaryCommand { get; }

    public BatchViewModel(
        ILogger logger,
        IBatchSchedulingService schedulingService,
        IPromptProvider promptProvider,
        IEnumerable<ModelConfig> models,
        CliOptions cliOptions,
        WeeklySummaryOrchestrator orchestrator,
        INewsBriefRunRepository newsBriefRepository,
        IUserSettingsService settingsService)
    {
        _logger = logger;
        _schedulingService = schedulingService;
        _promptProvider = promptProvider;
        _models = models.ToList();
        _orchestrator = orchestrator;
        _newsBriefRepository = newsBriefRepository;
        _settingsService = settingsService;

        var settings = settingsService.Load();
        _defaultModel = settings.DefaultModelId is not null
            ? _models.FirstOrDefault(m => m.ModelId == settings.DefaultModelId)
            : _models.FirstOrDefault();

        _disposables.Add(_slotTimer);
        _disposables.Add(_weeklyTimer);

        IsAutoSchedule = cliOptions.AutoSchedule;
        TodayDate = DateTime.Today.ToString("yyyy-MM-dd");

        WeeklySummaryInfo = new WeeklySummaryScheduleInfo(
            $"{WeeklySummaryDay} {WeeklySummaryTime:HH:mm} UTC",
            _defaultModel?.DisplayName);
        UpdateWeeklyNextRunDate();

        ToggleSchedulerCommand = new DelegateCommand(ToggleScheduler);
        RunWeeklySummaryCommand = new AsyncCommand(RunWeeklySummaryManualAsync,
            () => !IsBatchRunning && WeeklySummaryInfo.Status != SlotStatus.Running && _defaultModel is not null);

        // Build the visual schedule immediately so the user sees the day plan
        RebuildDaySlots();

        if (IsAutoSchedule)
        {
            StartScheduler();
        }
    }

    private void ToggleScheduler()
    {
        if (IsSchedulerActive)
            StopScheduler();
        else
            StartScheduler();
    }

    private void StartScheduler()
    {
        if (IsSchedulerActive) return;

        _logger.Info("Batch scheduler starting (interval: {0}h)", Interval.TotalHours);
        IsSchedulerActive = true;

        ScheduleNextSlot();
        ScheduleWeeklySummary();

        var nextWaiting = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
        SetStatus(nextWaiting is not null
            ? $"Scheduler started — next run at {nextWaiting.PlannedTime:HH:mm}, weekly summary {WeeklySummaryInfo.NextRunDate}"
            : $"Scheduler started — next run tomorrow at 00:00, weekly summary {WeeklySummaryInfo.NextRunDate}");
    }

    private void StopScheduler()
    {
        if (!IsSchedulerActive) return;

        _logger.Info("Batch scheduler stopping");

        _batchCts?.Cancel();
        _batchCts?.Dispose();
        _batchCts = null;

        _weeklyCts?.Cancel();
        _weeklyCts?.Dispose();
        _weeklyCts = null;

        _slotTimer.Disposable = null;
        _weeklyTimer.Disposable = null;

        // Reset any running slot back to waiting
        foreach (var slot in TodaySlots)
        {
            if (slot.Status == SlotStatus.Running)
            {
                slot.Status = SlotStatus.Waiting;
                slot.ProgressText = null;
            }
        }

        if (WeeklySummaryInfo.Status == SlotStatus.Running)
        {
            WeeklySummaryInfo.Status = SlotStatus.Waiting;
            WeeklySummaryInfo.ProgressText = null;
        }

        IsSchedulerActive = false;
        IsBatchRunning = false;
        SetStatus("Scheduler stopped");
    }

    /// <summary>
    /// Delegates day-slot generation to the scheduling service and maps to UI view models.
    /// </summary>
    private void RebuildDaySlots()
    {
        TodaySlots.Clear();
        TodayDate = DateTime.Today.ToString("yyyy-MM-dd");

        var planned = _schedulingService.BuildDaySlots(Interval);

        foreach (var slot in planned)
        {
            var status = slot.IsPast ? SlotStatus.Missed : SlotStatus.Waiting;
            TodaySlots.Add(new ScheduledSlot(slot.PlannedTime, status));
        }
    }

    /// <summary>
    /// Finds the next <see cref="SlotStatus.Waiting"/> slot and sets an Rx timer to fire at its planned time.
    /// When all today's slots are exhausted, schedules a day rollover to tomorrow's first slot.
    /// </summary>
    private void ScheduleNextSlot()
    {
        var now = DateTime.Now;
        var nextSlot = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);

        if (nextSlot is null)
        {
            ScheduleDayRollover();
            return;
        }

        var target = DateTime.Today + nextSlot.PlannedTime.ToTimeSpan();
        var delay = target - now;

        if (delay < TimeSpan.Zero)
        {
            // Slot time already passed — mark as missed and try the next one
            nextSlot.Status = SlotStatus.Missed;
            ScheduleNextSlot();
            return;
        }

        _logger.Debug("Next batch slot at {0} (in {1:F1} min)", nextSlot.PlannedTime, delay.TotalMinutes);

        _slotTimer.Disposable = _schedulingService
            .CreateTimer(delay)
            .Subscribe(_ => OnSlotFired(nextSlot));
    }

    /// <summary>
    /// All today's slots are done. Schedules a single Rx timer to midnight,
    /// rebuilds the day plan, and immediately fires the 00:00 slot.
    /// </summary>
    private void ScheduleDayRollover()
    {
        var delay = DateTime.Today.AddDays(1) - DateTime.Now;

        _logger.Debug("No more slots today — day rollover in {0:F1}h", delay.TotalHours);

        _slotTimer.Disposable = _schedulingService
            .CreateTimer(delay)
            .Subscribe(_ =>
            {
                _logger.Info("Day rollover — rebuilding daily schedule");
                RebuildDaySlots();
                SetStatus("New day — schedule reset");

                var firstSlot = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
                if (firstSlot is not null)
                    OnSlotFired(firstSlot);
            });
    }

    private async void OnSlotFired(ScheduledSlot slot)
    {
        if (!IsSchedulerActive) return;

        try
        {
            await ExecuteBatchRunAsync(slot);
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Batch run cancelled for slot {0}", slot.PlannedTime);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Batch run failed for slot {0}", slot.PlannedTime);
            slot.Status = SlotStatus.Failed;
            SetStatus($"Slot {slot.PlannedTime:HH:mm} failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBatchRunning = false;

            if (IsSchedulerActive)
            {
                ScheduleNextSlot();
            }
        }
    }

    private async Task ExecuteBatchRunAsync(ScheduledSlot slot)
    {
        IsBatchRunning = true;
        slot.Status = SlotStatus.Running;
        slot.ProgressText = $"0/{_models.Count}";

        SetStatus($"Slot {slot.PlannedTime:HH:mm} started — {_models.Count} models queued");

        _batchCts = new CancellationTokenSource();

        var progress = new Progress<BatchProgress>(p =>
        {
            if (p.CurrentModelName is not null)
            {
                slot.ProgressText = $"{p.CompletedModels}/{p.TotalModels}: {p.CurrentModelName}";
                SetStatus($"Slot {slot.PlannedTime:HH:mm} — model {p.CompletedModels + 1}/{p.TotalModels}: {p.CurrentModelName}...");
            }
        });

        try
        {
            var result = await _schedulingService.ExecuteSlotAsync(
                _models,
                _promptProvider.GetNewsBriefPrompt(),
                progress,
                _batchCts.Token);

            ApplySlotResult(slot, result);
        }
        finally
        {
            _batchCts?.Dispose();
            _batchCts = null;
        }
    }

    /// <summary>
    /// Maps a <see cref="BatchSlotResult"/> from the scheduling service onto the UI slot view model.
    /// </summary>
    private void ApplySlotResult(ScheduledSlot slot, BatchSlotResult result)
    {
        slot.SuccessCount = result.SuccessCount;
        slot.FailureCount = result.FailureCount;
        slot.TotalTokens = result.TotalTokens;
        slot.Duration = result.Duration;
        slot.ProgressText = null;
        slot.Status = result.HasFailures ? SlotStatus.Failed : SlotStatus.Completed;

        var nextWaiting = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
        var nextInfo = nextWaiting is not null ? $" — next at {nextWaiting.PlannedTime:HH:mm}" : "";

        SetStatus(result.HasFailures
            ? $"Slot {slot.PlannedTime:HH:mm} done — {result.SuccessCount} ok, {result.FailureCount} failed, {result.Duration.TotalMinutes:F1}min{nextInfo}"
            : $"Slot {slot.PlannedTime:HH:mm} complete — {result.SuccessCount} models, {result.TotalTokens:N0} tokens, {result.Duration.TotalMinutes:F1}min{nextInfo}",
            isError: result.HasFailures);
    }

    #region Weekly Summary

    /// <summary>
    /// Schedules the weekly summary timer to fire at the next Thursday 22:00 UTC.
    /// </summary>
    private void ScheduleWeeklySummary()
    {
        if (_defaultModel is null)
        {
            _logger.Warn("No default model configured — weekly summary scheduling skipped");
            return;
        }

        var delay = _schedulingService.CalculateWeeklyDelay(WeeklySummaryDay, WeeklySummaryTime, DateTime.UtcNow);
        UpdateWeeklyNextRunDate();

        _logger.Debug("Weekly summary scheduled — fires in {0:F1}h ({1})",
            delay.TotalHours, WeeklySummaryInfo.NextRunDate);

        _weeklyTimer.Disposable = _schedulingService
            .CreateTimer(delay)
            .Subscribe(_ => OnWeeklySummaryFired());
    }

    private async void OnWeeklySummaryFired()
    {
        if (!IsSchedulerActive) return;

        try
        {
            await ExecuteWeeklySummaryAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Weekly summary run cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Weekly summary run failed unexpectedly");
            WeeklySummaryInfo.Status = SlotStatus.Failed;
            SetStatus($"Weekly summary failed: {ex.Message}", isError: true);
        }
        finally
        {
            if (IsSchedulerActive)
                ScheduleWeeklySummary();
        }
    }

    /// <summary>
    /// Manual trigger for the weekly summary — runs independently of the scheduler toggle.
    /// </summary>
    private async Task RunWeeklySummaryManualAsync()
    {
        try
        {
            await ExecuteWeeklySummaryAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Manual weekly summary run cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Manual weekly summary run failed unexpectedly");
            WeeklySummaryInfo.Status = SlotStatus.Failed;
            SetStatus($"Weekly summary failed: {ex.Message}", isError: true);
        }
    }

    private async Task ExecuteWeeklySummaryAsync()
    {
        if (_defaultModel is null)
        {
            SetStatus("No default model configured. Set one in Settings.", isError: true);
            return;
        }

        WeeklySummaryInfo.Status = SlotStatus.Running;
        WeeklySummaryInfo.ProgressText = "Loading daily briefs...";
        SetStatus($"Weekly summary — loading daily briefs for {_defaultModel.DisplayName}...");

        _weeklyCts = new CancellationTokenSource();

        try
        {
            var settings = _settingsService.Load();
            var defaultModelId = settings.DefaultModelId ?? _defaultModel.ModelId;

            var allRuns = await _newsBriefRepository.GetByModelAsync(defaultModelId, _weeklyCts.Token);
            var successfulRuns = allRuns
                .Where(r => r.Status == RunStatus.Success && r.Item is not null)
                .ToList();

            if (successfulRuns.Count == 0)
            {
                WeeklySummaryInfo.Status = SlotStatus.Failed;
                WeeklySummaryInfo.ProgressText = null;
                SetStatus("Weekly summary — no successful daily briefs found for the default model", isError: true);
                return;
            }

            WeeklySummaryInfo.ProgressText = $"Running agent with {successfulRuns.Count} briefs...";
            SetStatus($"Weekly summary — running agent with {successfulRuns.Count} daily briefs...");

            var result = await _orchestrator.RunSummaryAsync(
                _defaultModel,
                _promptProvider.GetWeeklySummaryPrompt(),
                successfulRuns,
                _weeklyCts.Token);

            WeeklySummaryInfo.ProgressText = null;

            if (result.IsSuccess)
            {
                var run = result.Value;
                WeeklySummaryInfo.Status = SlotStatus.Completed;
                WeeklySummaryInfo.ThemeCount = run.Themes.Count;
                WeeklySummaryInfo.TotalTokens = run.TotalTokens;
                WeeklySummaryInfo.Duration = run.Duration;
                SetStatus($"Weekly summary complete — {run.Themes.Count} themes, {run.TotalTokens:N0} tokens, {run.Duration.TotalSeconds:F1}s");
            }
            else
            {
                WeeklySummaryInfo.Status = SlotStatus.Failed;
                SetStatus($"Weekly summary failed: {result.Errors.First().Message}", isError: true);
            }
        }
        finally
        {
            _weeklyCts?.Dispose();
            _weeklyCts = null;
        }
    }

    private void UpdateWeeklyNextRunDate()
    {
        var delay = _schedulingService.CalculateWeeklyDelay(WeeklySummaryDay, WeeklySummaryTime, DateTime.UtcNow);
        var nextRun = DateTime.UtcNow + delay;
        var daysUntil = (int)Math.Ceiling(delay.TotalDays);
        WeeklySummaryInfo.NextRunDate = $"{nextRun:MMM dd, yyyy} ({daysUntil}d)";
    }

    #endregion

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    public void Dispose()
    {
        _batchCts?.Cancel();
        _batchCts?.Dispose();
        _weeklyCts?.Cancel();
        _weeklyCts?.Dispose();
        _disposables.Dispose();
    }
}
