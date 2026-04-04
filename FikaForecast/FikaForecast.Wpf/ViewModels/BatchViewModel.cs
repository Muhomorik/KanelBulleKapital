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
/// Manages Rx.NET timers for daily time slots and delegates schedule building
/// and batch execution to <see cref="IBatchSchedulingService"/>.
/// Supports auto-start via CLI <c>--auto-schedule</c> and manual toggle from the UI.
/// </summary>
public class BatchViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(4);

    private readonly ILogger _logger;
    private readonly IBatchSchedulingService _schedulingService;
    private readonly IPromptProvider _promptProvider;
    private readonly IReadOnlyList<ModelConfig> _models;
    private readonly SynchronizationContextScheduler _uiScheduler;

    private readonly CompositeDisposable _disposables = new();
    private readonly SerialDisposable _slotTimer = new();
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
        IBatchSchedulingService schedulingService,
        IPromptProvider promptProvider,
        IEnumerable<ModelConfig> models,
        CliOptions cliOptions)
    {
        _logger = logger;
        _schedulingService = schedulingService;
        _promptProvider = promptProvider;
        _models = models.ToList();
        _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);

        _disposables.Add(_slotTimer);

        IsAutoSchedule = cliOptions.AutoSchedule;
        TodayDate = DateTime.Today.ToString("yyyy-MM-dd");

        ToggleSchedulerCommand = new DelegateCommand(ToggleScheduler);

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

        var nextWaiting = TodaySlots.FirstOrDefault(s => s.Status == SlotStatus.Waiting);
        SetStatus(nextWaiting is not null
            ? $"Scheduler started — next run at {nextWaiting.PlannedTime:HH:mm}"
            : "Scheduler started — next run tomorrow at 00:00");
    }

    private void StopScheduler()
    {
        if (!IsSchedulerActive) return;

        _logger.Info("Batch scheduler stopping");

        _batchCts?.Cancel();
        _batchCts?.Dispose();
        _batchCts = null;

        _slotTimer.Disposable = null;

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

        _slotTimer.Disposable = Observable
            .Timer(delay, _uiScheduler)
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

        _slotTimer.Disposable = Observable
            .Timer(delay, _uiScheduler)
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
