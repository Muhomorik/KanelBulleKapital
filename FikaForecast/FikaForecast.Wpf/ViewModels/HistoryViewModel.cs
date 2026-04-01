using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Wpf.Views;
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
    public ICommand DeleteRunCommand { get; }
    public ICommand DeleteAllRunsCommand { get; }

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
        DeleteRunCommand = new AsyncCommand<NewsBriefRun>(DeleteRunAsync);
        DeleteAllRunsCommand = new AsyncCommand(DeleteAllRunsAsync);

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

    /// <summary>Deletes a single run after confirmation.</summary>
    private async Task DeleteRunAsync(NewsBriefRun? run)
    {
        if (run == null)
            return;

        var dialog = new ConfirmationDialog(
            $"Delete run from {run.Timestamp:yyyy-MM-dd HH:mm}? This cannot be undone.");
        var confirmed = dialog.ShowDialog() ?? false;

        if (!confirmed)
            return;

        try
        {
            await _repository.DeleteAsync(run.RunId);
            Runs.Remove(run);
            if (SelectedRun == run)
                SelectedRun = Runs.Count > 0 ? Runs[0] : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete run");
        }
    }

    /// <summary>Deletes all runs after confirmation.</summary>
    private async Task DeleteAllRunsAsync()
    {
        var dialog = new ConfirmationDialog("Delete all runs? This cannot be undone.");
        var confirmed = dialog.ShowDialog() ?? false;

        if (!confirmed)
            return;

        try
        {
            await _repository.DeleteAllAsync();
            Runs.Clear();
            SelectedRun = null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete all runs");
        }
    }
}
