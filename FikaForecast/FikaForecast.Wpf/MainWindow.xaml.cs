using FikaForecast.Wpf.ViewModels;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf;

/// <summary>
/// Main application window. DataContext is set to <see cref="MainWindowViewModel"/> via DI.
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
