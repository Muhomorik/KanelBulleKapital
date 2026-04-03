using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

using DevExpress.Mvvm;

using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;

using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Batch Scheduler tab.
/// Runs all models sequentially at fixed daily time slots (every 4 hours) using Rx.NET.
/// Missed slots are ignored. Resets at midnight and schedules the next day.
/// Supports auto-start via CLI <c>--auto-schedule</c> and manual toggle from the UI.
/// </summary>
public class BatchViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(4);

    private readonly ILogger _logger;
    private readonly IBriefComparisonService _comparisonService;
    private readonly IPromptProvider _promptProvider;
    private readonly IReadOnlyList<ModelConfig> _models;
    private readonly SynchronizationContextScheduler _uiScheduler;

    private readonly CompositeDisposable _disposables = new();
    private readonly SerialDisposable _slotTimer = new();
    private readonly SerialDisposable _midnightTimer = new();
    private CancellationTokenSource? _batchCts;

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

    public ICommand ToggleSchedulerCommand { get; }

    public BatchViewModel(
        ILogger logger,
        IBriefComparisonService comparisonService,
        IPromptProvider promptProvider,
        IEnumerable<ModelConfig> models,
        CliOptions cliOptions)
    {
        _logger = logger;
        _comparisonService = comparisonService;
        _promptProvider = promptProvider;
        _models = models.ToList();
        _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);

        _disposables.Add(_slotTimer);
        _disposables.Add(_midnightTimer);

        IsAutoSchedule = cliOptions.AutoSchedule;
        TodayDate = DateTime.Today.ToString("yyyy-MM-dd");

        ToggleSchedulerCommand = new DelegateCommand(ToggleScheduler);

        // Build the visual schedule immediately so the user sees the day plan
        BuildDaySlots();

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
        ScheduleMidnightReset();

        var nextWaiting = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
        SetStatus(nextWaiting is not null
            ? $"Scheduler started — next run at {nextWaiting.PlannedTime:HH:mm}"
            : "Scheduler started — no more slots today, waiting for midnight reset");
    }

    private void StopScheduler()
    {
        if (!IsSchedulerActive) return;

        _logger.Info("Batch scheduler stopping");

        _batchCts?.Cancel();
        _batchCts?.Dispose();
        _batchCts = null;

        _slotTimer.Disposable = null;
        _midnightTimer.Disposable = null;

        // Reset any running slot back to waiting
        foreach (var slot in TodaySlots)
        {
            if (slot.Status == SlotStatus.Running)
            {
                slot.Status = SlotStatus.Waiting;
                slot.ProgressText = null;
            }
        }

        IsSchedulerActive = false;
        IsBatchRunning = false;
        SetStatus("Scheduler stopped");
    }

    /// <summary>
    /// Generates the 6 daily time slots (00:00, 04:00, 08:00, 12:00, 16:00, 20:00).
    /// Past slots are immediately marked <see cref="SlotStatus.Missed"/>.
    /// </summary>
    private void BuildDaySlots()
    {
        TodaySlots.Clear();
        TodayDate = DateTime.Today.ToString("yyyy-MM-dd");

        var now = TimeOnly.FromDateTime(DateTime.Now);

        for (var hour = 0; hour < 24; hour += (int)Interval.TotalHours)
        {
            var time = new TimeOnly(hour, 0);
            var status = time < now ? SlotStatus.Missed : SlotStatus.Waiting;
            TodaySlots.Add(new ScheduledSlot(time, status));
        }
    }

    /// <summary>
    /// Finds the next <see cref="SlotStatus.Waiting"/> slot and sets an Rx timer to fire at its planned time.
    /// </summary>
    private void ScheduleNextSlot()
    {
        var now = DateTime.Now;
        var nextSlot = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);

        if (nextSlot is null)
        {
            _logger.Debug("No more waiting slots today");
            _slotTimer.Disposable = null;
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

        _slotTimer.Disposable = Observable
            .Timer(delay, _uiScheduler)
            .Subscribe(_ => OnSlotFired(nextSlot));
    }

    /// <summary>
    /// Schedules a timer that fires just after midnight to reset the daily plan.
    /// </summary>
    private void ScheduleMidnightReset()
    {
        var midnight = DateTime.Today.AddDays(1);
        var delay = midnight - DateTime.Now + TimeSpan.FromSeconds(1); // tiny buffer past midnight

        _logger.Debug("Midnight reset scheduled in {0:F1}h", delay.TotalHours);

        _midnightTimer.Disposable = Observable
            .Timer(delay, _uiScheduler)
            .Subscribe(_ =>
            {
                _logger.Info("Midnight reset — rebuilding daily schedule");
                BuildDaySlots();

                if (IsSchedulerActive)
                {
                    ScheduleNextSlot();
                    ScheduleMidnightReset();
                    SetStatus("New day — schedule reset");
                }
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

        var startTime = DateTime.Now;
        SetStatus($"Slot {slot.PlannedTime:HH:mm} started — {_models.Count} models queued");
        _logger.Info("Batch slot {0} started — {1} models", slot.PlannedTime, _models.Count);

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
            var results = await _comparisonService.CompareSequentiallyAsync(
                _models,
                _promptProvider.GetNewsBriefPrompt(),
                progress,
                _batchCts.Token);

            var successes = results.Count(r => r.IsSuccess);
            var failures = results.Count(r => r.IsFailed);
            var totalTokens = results.Where(r => r.IsSuccess).Sum(r => r.Value.TotalTokens);
            var elapsed = DateTime.Now - startTime;

            slot.SuccessCount = successes;
            slot.FailureCount = failures;
            slot.TotalTokens = totalTokens;
            slot.Duration = elapsed;
            slot.ProgressText = null;
            slot.Status = failures == 0 ? SlotStatus.Completed : SlotStatus.Failed;

            var nextWaiting = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
            var nextInfo = nextWaiting is not null ? $" — next at {nextWaiting.PlannedTime:HH:mm}" : "";

            SetStatus(failures == 0
                ? $"Slot {slot.PlannedTime:HH:mm} complete — {successes} models, {totalTokens:N0} tokens, {elapsed.TotalMinutes:F1}min{nextInfo}"
                : $"Slot {slot.PlannedTime:HH:mm} done — {successes} ok, {failures} failed, {elapsed.TotalMinutes:F1}min{nextInfo}",
                isError: failures > 0);

            _logger.Info("Batch slot {0} complete — {1}/{2} succeeded, {3} tokens",
                slot.PlannedTime, successes, _models.Count, totalTokens);
        }
        finally
        {
            _batchCts?.Dispose();
            _batchCts = null;
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    public void Dispose()
    {
        _batchCts?.Cancel();
        _batchCts?.Dispose();
        _disposables.Dispose();
    }
}
