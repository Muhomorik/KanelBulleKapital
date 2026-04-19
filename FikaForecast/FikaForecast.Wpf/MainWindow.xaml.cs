using System.Windows;
using FikaForecast.Wpf.ViewModels;
using FikaForecast.Wpf.Views;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf;

/// <summary>
/// Main application window. DataContext is set to <see cref="MainWindowViewModel"/> via DI.
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly Func<SettingsWindow> _settingsFactory;
    private readonly Func<ExportWindow> _exportFactory;

    public MainWindow(
        MainWindowViewModel viewModel,
        Func<SettingsWindow> settingsFactory,
        Func<ExportWindow> exportFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsFactory = settingsFactory;
        _exportFactory = exportFactory;
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsFactory();
        settings.Owner = this;
        settings.ShowDialog();
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var export = _exportFactory();
        export.Owner = this;
        export.ShowDialog();
    }
}
