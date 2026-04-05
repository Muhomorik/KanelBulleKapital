using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;
using FluentResults;
using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Evaluation tab. Loads all past runs with checkboxes (default model + last 7 days
/// are pre-checked), and evaluates them against prompt rules using an LLM as judge.
/// </summary>
public class EvaluationViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly INewsBriefRunRepository _repository;
    private readonly IEvaluationAgent _evaluationAgent;
    private readonly IPromptProvider _promptProvider;
    private readonly string? _defaultModelId;
    private readonly ModelConfig? _defaultModel;

    public ObservableCollection<EvaluationRunItem> Runs { get; } = [];

    public EvaluationRunItem? SelectedRun
    {
        get => GetValue<EvaluationRunItem?>();
        set => SetValue(value);
    }

    public string? EvaluationModelName { get; }

    public string? EvaluationResult
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public bool IsEvaluating
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public string? StatusMessage
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public bool IsStatusError
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public ICommand LoadRunsCommand { get; }
    public ICommand EvaluateCommand { get; }

    /// <summary>Runtime constructor (DI).</summary>
    public EvaluationViewModel(
        ILogger logger,
        INewsBriefRunRepository repository,
        IUserSettingsService settingsService,
        IEvaluationAgent evaluationAgent,
        IPromptProvider promptProvider,
        IEnumerable<ModelConfig> models)
    {
        _logger = logger;
        _repository = repository;
        _evaluationAgent = evaluationAgent;
        _promptProvider = promptProvider;

        var settings = settingsService.Load();
        _defaultModelId = settings.DefaultModelId;
        _defaultModel = models.FirstOrDefault(m => m.ModelId == _defaultModelId);

        EvaluationModelName = _defaultModel?.DisplayName;

        LoadRunsCommand = new AsyncCommand(LoadRunsAsync);
        EvaluateCommand = new AsyncCommand(EvaluateAsync, CanEvaluate);

        // Load initial runs when view opens
        ((AsyncCommand)LoadRunsCommand).Execute(null);
    }

    /// <summary>Design-time constructor.</summary>
    public EvaluationViewModel()
    {
        _logger = null!;
        _repository = null!;
        _evaluationAgent = null!;
        _promptProvider = null!;

        LoadRunsCommand = new DelegateCommand(() => { });
        EvaluateCommand = new DelegateCommand(() => { });
    }

    /// <summary>
    /// Loads all runs from the repository. Pre-checks runs matching the default model and last 7 days.
    /// </summary>
    private async Task LoadRunsAsync()
    {
        try
        {
            var allRuns = await _repository.GetAllAsync();
            var cutoff = DateTimeOffset.Now.AddDays(-7);

            Runs.Clear();
            foreach (var run in allRuns)
            {
                var isDefault = !string.IsNullOrEmpty(_defaultModelId)
                                && run.ModelId == _defaultModelId
                                && run.Timestamp >= cutoff;

                Runs.Add(new EvaluationRunItem(run, isDefault));
            }

            SelectedRun = Runs.FirstOrDefault(r => r.IsChecked) ?? Runs.FirstOrDefault();
            EvaluationResult = null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load runs for evaluation");
        }
    }

    /// <summary>
    /// Evaluates checked runs. Single checked run uses standard evaluation;
    /// multiple checked runs use the comparison prompt to rank models.
    /// </summary>
    private async Task EvaluateAsync()
    {
        var checkedItems = Runs.Where(r => r.IsChecked).ToList();

        // No checked runs — fall back to the selected (clicked) run
        if (checkedItems.Count == 0 && SelectedRun?.Run is not null)
            checkedItems = [SelectedRun];

        if (checkedItems.Count == 0)
            return;

        var model = _defaultModel;
        if (model is null)
        {
            SetStatus("No default model configured. Set one in Settings.", isError: true);
            return;
        }

        IsEvaluating = true;
        EvaluationResult = null;

        try
        {
            if (checkedItems.Count == 1)
            {
                SetStatus($"Evaluating with {model.DisplayName}...");

                var result = await _evaluationAgent.EvaluateAsync(
                    model,
                    _promptProvider.GetEvaluationPrompt(),
                    checkedItems[0].Run.RawAgentOutput,
                    _promptProvider.GetNewsBriefPrompt());

                HandleResult(result);
            }
            else
            {
                var modelNames = string.Join(", ", checkedItems.Select(r => r.Run.DeploymentName).Distinct());
                SetStatus($"Comparing {checkedItems.Count} reports ({modelNames}) with {model.DisplayName}...");

                var reports = checkedItems
                    .Select(r => (r.Run.DeploymentName, r.Run.RawAgentOutput))
                    .ToList();

                var result = await _evaluationAgent.CompareAsync(
                    model,
                    _promptProvider.GetComparisonPrompt(),
                    reports,
                    _promptProvider.GetNewsBriefPrompt());

                HandleResult(result);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Evaluation failed unexpectedly");
            SetStatus($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            IsEvaluating = false;
        }
    }

    private bool CanEvaluate() => !IsEvaluating && (Runs.Any(r => r.IsChecked) || SelectedRun?.Run is not null);

    private void HandleResult(Result<AgentResult> result)
    {
        if (result.IsSuccess)
        {
            var agentResult = result.Value;
            EvaluationResult = agentResult.RawOutput;
            var totalTokens = agentResult.InputTokens + agentResult.OutputTokens;
            SetStatus($"Evaluation complete — {totalTokens} tokens, {agentResult.Duration.TotalSeconds:F1}s");
        }
        else
        {
            SetStatus($"Evaluation failed: {result.Errors.First().Message}", isError: true);
        }
    }

    private void SetStatus(string? message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
