using FikaForecast.Wpf.ViewModels;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf;

/// <summary>
/// Main application window. Receives ViewModels via constructor injection.
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow(ComparisonViewModel comparisonVm, HistoryViewModel historyVm)
    {
        InitializeComponent();

        ComparisonView.DataContext = comparisonVm;
        HistoryView.DataContext = historyVm;
    }
}
