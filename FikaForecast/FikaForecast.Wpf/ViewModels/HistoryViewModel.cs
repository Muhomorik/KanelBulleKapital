using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the run history view. Loads past runs from SQLite and supports model filtering.
/// </summary>
/// <remarks>
/// Must be created on the UI thread. Async commands dispatch back to the UI thread automatically.
/// </remarks>
public class HistoryViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly INewsBriefRunRepository _repository;

    public ObservableCollection<NewsBriefRun> Runs { get; } = [];

    public NewsBriefRun? SelectedRun
    {
        get => GetValue<NewsBriefRun?>();
        set => SetValue(value);
    }

    public string? FilterModelId
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public ICommand LoadRunsCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public HistoryViewModel(ILogger logger, INewsBriefRunRepository repository)
    {
        _logger = logger;
        _repository = repository;

        LoadRunsCommand = new AsyncCommand(LoadRunsAsync);
        ClearFilterCommand = new DelegateCommand(() =>
        {
            FilterModelId = null;
            ((AsyncCommand)LoadRunsCommand).Execute(null);
        });

        // Load initial runs when view opens
        ((AsyncCommand)LoadRunsCommand).Execute(null);
    }

    /// <summary>Loads runs from the repository, optionally filtered by <see cref="FilterModelId"/>.</summary>
    private async Task LoadRunsAsync()
    {
        try
        {
            var runs = string.IsNullOrEmpty(FilterModelId)
                ? await _repository.GetAllAsync()
                : await _repository.GetByModelAsync(FilterModelId);

            Runs.Clear();
            foreach (var run in runs)
                Runs.Add(run);

            // Auto-select the first run
            SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load history");
        }
    }
}
