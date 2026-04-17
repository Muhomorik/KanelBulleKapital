using FikaForecast.Wpf.ViewModels;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf.Views;

/// <summary>
/// Settings window. DataContext is set to <see cref="SettingsViewModel"/> via DI.
/// </summary>
public partial class SettingsWindow : MetroWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
