using DevExpress.Mvvm;
using NLog;

namespace FikaFinans.Wpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger? _logger;

    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value, nameof(Title));
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value, nameof(Subtitle));
    }

    /// <summary>Runtime constructor (DI). Chains to the parameterless ctor so <see cref="OnInitializeInRuntime"/> fires.</summary>
    public MainWindowViewModel(ILogger logger) : this()
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.Info("MainWindowViewModel initialized");
    }

    /// <summary>Design-time constructor — required for <c>d:DataContext IsDesignTimeCreatable=True</c>.</summary>
    public MainWindowViewModel()
    {
    }

    protected override void OnInitializeInDesignMode()
    {
        base.OnInitializeInDesignMode();
        Title = "FikaFinans (Design)";
        Subtitle = "Design-time preview. Bindings resolve against this instance.";
    }

    protected override void OnInitializeInRuntime()
    {
        base.OnInitializeInRuntime();
        Title = "FikaFinans";
        Subtitle = "DI + NLog + MahApps wired. Time to build something.";
    }
}
