using System.Windows.Controls;
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

        // PasswordBox.Password isn't a DependencyProperty — seed and sync manually.
        Loaded += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm && vm.SyncAuthToken is not null)
                SyncTokenBox.Password = vm.SyncAuthToken;
        };
    }

    private void SyncTokenBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.SyncAuthToken = ((PasswordBox)sender).Password;
    }
}
