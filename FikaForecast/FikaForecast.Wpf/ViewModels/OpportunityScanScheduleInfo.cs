using DevExpress.Mvvm;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Bindable state for the opportunity scan entry in the batch scheduler UI.
/// Displays the scheduled day/time, current status, and result details.
/// </summary>
public class OpportunityScanScheduleInfo : ViewModelBase
{
    /// <summary>Display label for the planned schedule, e.g. "Thursday 22:20 UTC".</summary>
    public string PlannedDay { get; }

    /// <summary>Default model display name used for the opportunity scan.</summary>
    public string? ModelName { get; }

    public SlotStatus Status
    {
        get => GetValue<SlotStatus>();
        set => SetValue(value);
    }

    /// <summary>Progress text shown while running (e.g. "Loading chains...", "Running agent...").</summary>
    public string? ProgressText
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>Number of rotation targets returned by the opportunity scan agent.</summary>
    public int TargetCount
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public int TotalTokens
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public TimeSpan Duration
    {
        get => GetValue<TimeSpan>();
        set => SetValue(value);
    }

    /// <summary>Human-readable next run date, e.g. "Apr 09, 2026 (4 days)".</summary>
    public string? NextRunDate
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public OpportunityScanScheduleInfo(string plannedDay, string? modelName)
    {
        PlannedDay = plannedDay;
        ModelName = modelName;
        Status = SlotStatus.Waiting;
    }
}
