using DevExpress.Mvvm;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Bindable state for the substitution chain entry in the batch scheduler UI.
/// Displays the scheduled day/time, current status, and result details.
/// </summary>
public class SubstitutionChainScheduleInfo : ViewModelBase
{
    /// <summary>Display label for the planned schedule, e.g. "Thursday 22:10 UTC".</summary>
    public string PlannedDay { get; }

    /// <summary>Default model display name used for the substitution chain.</summary>
    public string? ModelName { get; }

    public SlotStatus Status
    {
        get => GetValue<SlotStatus>();
        set => SetValue(value);
    }

    /// <summary>Progress text shown while running (e.g. "Loading summary...", "Running agent...").</summary>
    public string? ProgressText
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>Number of rotation chains returned by the substitution chain agent.</summary>
    public int ChainCount
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

    public SubstitutionChainScheduleInfo(string plannedDay, string? modelName)
    {
        PlannedDay = plannedDay;
        ModelName = modelName;
        Status = SlotStatus.Waiting;
    }
}
