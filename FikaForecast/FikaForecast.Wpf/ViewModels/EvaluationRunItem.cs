using DevExpress.Mvvm;
using FikaForecast.Domain.Entities;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Wraps a <see cref="NewsBriefRun"/> with an <see cref="IsChecked"/> flag
/// for the evaluation view's checkbox list.
/// </summary>
public class EvaluationRunItem : ViewModelBase
{
    public NewsBriefRun Run { get; }

    public bool IsChecked
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public EvaluationRunItem(NewsBriefRun run, bool isChecked)
    {
        Run = run;
        IsChecked = isChecked;
    }
}
