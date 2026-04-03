using DevExpress.Mvvm;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Status of a single scheduled time slot within the daily batch plan.
/// </summary>
public enum SlotStatus
{
    Waiting,
    Missed,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Represents one fixed time slot in the daily batch schedule (e.g. 08:00, 12:00, ...).
/// Notifies the UI when status or results change.
/// </summary>
public class ScheduledSlot : ViewModelBase
{
    public TimeOnly PlannedTime { get; }

    public SlotStatus Status
    {
        get => GetValue<SlotStatus>();
        set => SetValue(value);
    }

    /// <summary>Number of successful models in this slot's batch run.</summary>
    public int SuccessCount
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    /// <summary>Number of failed models in this slot's batch run.</summary>
    public int FailureCount
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    /// <summary>Total tokens consumed across all models in this slot.</summary>
    public int TotalTokens
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    /// <summary>How long the batch run took.</summary>
    public TimeSpan Duration
    {
        get => GetValue<TimeSpan>();
        set => SetValue(value);
    }

    /// <summary>Progress text shown while the slot is running (e.g. "2/4: GPT-4.1").</summary>
    public string? ProgressText
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public int ModelCount => SuccessCount + FailureCount;

    public ScheduledSlot(TimeOnly plannedTime, SlotStatus initialStatus = SlotStatus.Waiting)
    {
        PlannedTime = plannedTime;
        Status = initialStatus;
    }
}
