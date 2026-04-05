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
    public EvaluationViewModel EvaluationVm { get; }
    public BatchViewModel BatchVm { get; }
    public WeeklySummaryViewModel WeeklySummaryVm { get; }
    public SubstitutionChainViewModel SubstitutionChainVm { get; }
    public OpportunityScanViewModel OpportunityScanVm { get; }

    /// <summary>Runtime constructor (DI).</summary>
    public MainWindowViewModel(
        ILogger logger,
        ComparisonViewModel comparisonVm,
        HistoryViewModel historyVm,
        EvaluationViewModel evaluationVm,
        BatchViewModel batchVm,
        WeeklySummaryViewModel weeklySummaryVm,
        SubstitutionChainViewModel substitutionChainVm,
        OpportunityScanViewModel opportunityScanVm)
    {
        ComparisonVm = comparisonVm ?? throw new ArgumentNullException(nameof(comparisonVm));
        HistoryVm = historyVm ?? throw new ArgumentNullException(nameof(historyVm));
        EvaluationVm = evaluationVm ?? throw new ArgumentNullException(nameof(evaluationVm));
        BatchVm = batchVm ?? throw new ArgumentNullException(nameof(batchVm));
        WeeklySummaryVm = weeklySummaryVm ?? throw new ArgumentNullException(nameof(weeklySummaryVm));
        SubstitutionChainVm = substitutionChainVm ?? throw new ArgumentNullException(nameof(substitutionChainVm));
        OpportunityScanVm = opportunityScanVm ?? throw new ArgumentNullException(nameof(opportunityScanVm));
    }

    /// <summary>Design-time constructor.</summary>
    public MainWindowViewModel()
    {
        ComparisonVm = null!;
        HistoryVm = null!;
        EvaluationVm = null!;
        BatchVm = null!;
        WeeklySummaryVm = null!;
        SubstitutionChainVm = null!;
        OpportunityScanVm = null!;
    }
}
