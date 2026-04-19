using FikaForecast.Wpf.ViewModels;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf.Views;

/// <summary>
/// Export window. DataContext is set to <see cref="ExportViewModel"/> via DI.
/// </summary>
public partial class ExportWindow : MetroWindow
{
    public ExportWindow(ExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
