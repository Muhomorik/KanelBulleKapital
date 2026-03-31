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

    public MainWindow(MainWindowViewModel viewModel, Func<SettingsWindow> settingsFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsFactory = settingsFactory;
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsFactory();
        settings.Owner = this;
        settings.ShowDialog();
    }
}
