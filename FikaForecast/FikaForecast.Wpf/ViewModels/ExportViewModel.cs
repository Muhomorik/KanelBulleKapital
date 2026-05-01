using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Wpf.Services;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Export window: manages the export folder path, the auto-export toggle,
/// and the manual "export this week" action.
/// </summary>
public class ExportViewModel : ViewModelBase
{
    private readonly IUserSettingsService _settingsService;
    private readonly IWeeklyReportExporter _exporter;
    private readonly IWeeklySummaryRunRepository _weeklyRepo;
    private readonly IFolderPicker _folderPicker;
    private readonly ILogger _logger;

    private ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    public ObservableCollection<WeekOption> WeekOptions { get; } = [];

    public string? ExportFolderPath
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public bool AutoExportEnabled
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public WeekOption? SelectedWeek
    {
        get => GetValue<WeekOption?>();
        set => SetValue(value, () => ExportCommand.RaiseCanExecuteChanged());
    }

    public string? StatusText
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public bool IsExporting
    {
        get => GetValue<bool>();
        set => SetValue(value, () => ExportCommand.RaiseCanExecuteChanged());
    }

    public DelegateCommand BrowseCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public AsyncCommand ExportCommand { get; }

    public ExportViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        IWeeklyReportExporter exporter,
        IWeeklySummaryRunRepository weeklyRepo,
        IFolderPicker folderPicker)
    {
        _logger = logger;
        _settingsService = settingsService;
        _exporter = exporter;
        _weeklyRepo = weeklyRepo;
        _folderPicker = folderPicker;

        var current = settingsService.Load();
        ExportFolderPath = current.ExportFolderPath;
        AutoExportEnabled = current.AutoExportEnabled;

        BrowseCommand = new DelegateCommand(Browse);
        SaveCommand = new DelegateCommand(Save);
        CloseCommand = new DelegateCommand(() => CurrentWindowService?.Close());
        ExportCommand = new AsyncCommand(RunExportAsync, () => SelectedWeek is not null && !IsExporting);

        _ = LoadWeeksAsync();
    }

    /// <summary>Design-time constructor.</summary>
    public ExportViewModel()
    {
        _logger = null!;
        _settingsService = null!;
        _exporter = null!;
        _weeklyRepo = null!;
        _folderPicker = null!;
        BrowseCommand = new DelegateCommand(() => { });
        SaveCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        ExportCommand = new AsyncCommand(() => Task.CompletedTask);
    }

    private void Browse()
    {
        var chosen = _folderPicker.Pick(ExportFolderPath);
        if (!string.IsNullOrWhiteSpace(chosen))
            ExportFolderPath = chosen;
    }

    private void Save()
    {
        PersistSettings();
        CurrentWindowService?.Close();
    }

    private void PersistSettings()
    {
        var settings = _settingsService.Load();
        settings.ExportFolderPath = string.IsNullOrWhiteSpace(ExportFolderPath) ? null : ExportFolderPath;
        settings.AutoExportEnabled = AutoExportEnabled;
        _settingsService.Save(settings);
        _logger.Info("Export settings saved (auto={0}, path={1})", AutoExportEnabled, ExportFolderPath ?? "<none>");
    }

    private async Task LoadWeeksAsync()
    {
        try
        {
            var runs = await _weeklyRepo.GetAllAsync(CancellationToken.None);
            var weeks = runs
                .Select(r => (r.PeriodStart, r.PeriodEnd, Iso: IsoWeek.Compute(r.PeriodStart)))
                .GroupBy(x => (x.Iso.Year, x.Iso.Week))
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.PeriodStart).First();
                    return new WeekOption(latest.PeriodStart, latest.PeriodEnd);
                })
                .OrderByDescending(o => (o.IsoYear, o.IsoWeekNumber))
                .ToList();

            WeekOptions.Clear();
            foreach (var w in weeks) WeekOptions.Add(w);
            SelectedWeek = WeekOptions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load weekly runs for export dropdown");
            StatusText = "Failed to load weeks. Check logs.";
        }
    }

    private async Task RunExportAsync()
    {
        if (SelectedWeek is null) return;
        if (string.IsNullOrWhiteSpace(ExportFolderPath))
        {
            StatusText = "Set an export folder and click Save first.";
            return;
        }

        IsExporting = true;
        StatusText = "Exporting…";
        try
        {
            // Persist first so the exporter reads a path from disk that matches the UI.
            PersistSettings();

            var written = await _exporter.ExportAllForWeekAsync(SelectedWeek.WeekStart, CancellationToken.None);
            StatusText = written.Count == 0
                ? "No successful runs found for this week."
                : $"Exported {written.Count} file(s) to {ExportFolderPath}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Manual export failed");
            StatusText = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }
}
