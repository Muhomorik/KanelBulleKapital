using DevExpress.Mvvm;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    public DelegateCommand CloseCommand => new(() => CurrentWindowService?.Close());

    /// <summary>Runtime constructor (DI).</summary>
    public SettingsViewModel(ILogger logger)
    {
    }

    /// <summary>Design-time constructor.</summary>
    public SettingsViewModel()
    {
    }
}
