using System.Reactive.Concurrency;
using DevExpress.Mvvm;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Top-level ViewModel for the main window. Holds child ViewModels for each tab.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    public ComparisonViewModel ComparisonVm { get; }
    public HistoryViewModel HistoryVm { get; }

    /// <summary>Runtime constructor (DI).</summary>
    public MainWindowViewModel(
        ILogger logger,
        ComparisonViewModel comparisonVm,
        HistoryViewModel historyVm)
    {
        ComparisonVm = comparisonVm ?? throw new ArgumentNullException(nameof(comparisonVm));
        HistoryVm = historyVm ?? throw new ArgumentNullException(nameof(historyVm));
    }

    /// <summary>Design-time constructor.</summary>
    public MainWindowViewModel()
    {
        ComparisonVm = null!;
        HistoryVm = null!;
    }
}
