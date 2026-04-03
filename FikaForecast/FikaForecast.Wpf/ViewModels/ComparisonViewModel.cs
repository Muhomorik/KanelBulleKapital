using System.Collections.ObjectModel;
using System.Windows.Input;

using DevExpress.Mvvm;

using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Wpf.Services;

using FluentResults;

using Microsoft.Extensions.Configuration;

using NLog;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// ViewModel for the side-by-side model comparison view.
/// Runs the News Brief Agent against one or more models and displays results.
/// </summary>
/// <remarks>
/// Must be created on the UI thread. Async commands dispatch back to the UI thread automatically.
/// </remarks>
public class ComparisonViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IBriefComparisonService _comparisonService;
    private readonly NewsBriefOrchestrator _orchestrator;
    private readonly IPromptProvider _promptProvider;

    public ObservableCollection<ModelConfig> AvailableModels { get; } = [];

    public ObservableCollection<NewsBriefRun> Results { get; } = [];

    public ModelConfig? SelectedModel
    {
        get => GetValue<ModelConfig?>();
        set => SetValue(value);
    }

    public bool IsRunning
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

    /// <summary>True when Azure AI Foundry endpoint and API key are configured.</summary>
    public bool IsConfigured { get; }

    /// <summary>User-facing message explaining what configuration is missing.</summary>
    public string? ConfigurationMessage { get; }

    public ICommand RunComparisonCommand { get; }
    public ICommand RunSingleCommand { get; }
    public ICommand ClearResultsCommand { get; }

    public ComparisonViewModel(
        ILogger logger,
        IConfiguration configuration,
        IBriefComparisonService comparisonService,
        NewsBriefOrchestrator orchestrator,
        IEnumerable<ModelConfig> models,
        IUserSettingsService settingsService,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _comparisonService = comparisonService;
        _orchestrator = orchestrator;
        _promptProvider = promptProvider;

        var settings = settingsService.Load();
        var hasSettings = settings.EnabledModelIds.Count > 0;

        foreach (var model in models)
        {
            if (!hasSettings || settings.EnabledModelIds.Contains(model.ModelId))
            {
                AvailableModels.Add(model);
            }
        }

        // Pre-select default model.
        if (settings.DefaultModelId is not null)
        {
            SelectedModel = AvailableModels.FirstOrDefault(m => m.ModelId == settings.DefaultModelId);
        }

        // Check configuration
        var (isConfigured, message) = CheckConfiguration(configuration);
        IsConfigured = isConfigured;
        ConfigurationMessage = message;

        if (!isConfigured)
        {
            _logger.Warn("Azure AI Foundry not configured: {0}", message);
        }

        RunComparisonCommand = new AsyncCommand(RunComparisonAsync, () => !IsRunning && AvailableModels.Count >= 2);
        RunSingleCommand = new AsyncCommand<ModelConfig>(RunSingleAsync, _ => !IsRunning);
        ClearResultsCommand = new DelegateCommand(() => { Results.Clear(); SetStatus(null); }, () => Results.Count > 0);
    }

    private static (bool IsConfigured, string? Message) CheckConfiguration(IConfiguration configuration)
    {
        var missing = new List<string>();

        if (string.IsNullOrEmpty(configuration["AzureAIFoundry:ProjectEndpoint"]))
        {
            missing.Add("AzureAIFoundry:ProjectEndpoint");
        }

        // ApiKey is no longer needed — DefaultAzureCredential handles auth via VS/Windows login

        if (missing.Count == 0)
        {
            return (true, null);
        }

        var message = $"Azure AI Foundry is not configured. Missing: {string.Join(", ", missing)}.\n\n"
                      + "To set up, run this command in the FikaForecast.Wpf project folder:\n\n"
                      + "  dotnet user-secrets set \"AzureAIFoundry:ProjectEndpoint\" \"https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>\"\n\n"
                      + "Authentication uses your Visual Studio / Windows login (DefaultAzureCredential).\n\n"
                      + "See docs/SECRETS-MANAGEMENT.md for details.";

        return (false, message);
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    /// <summary>Runs all selected models in parallel and adds results to the collection.</summary>
    private async Task RunComparisonAsync()
    {
        IsRunning = true;
        SetStatus($"Running comparison across {AvailableModels.Count} models...");
        Results.Clear();

        try
        {
            var results = await _comparisonService.CompareAsync(
                AvailableModels.ToList(),
                _promptProvider.GetNewsBriefPrompt());

            var successes = 0;
            var failures = new List<string>();

            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    Results.Add(result.Value);
                    successes++;
                }
                else
                {
                    failures.Add(result.Errors.First().Message);
                }
            }

            if (failures.Count == 0)
            {
                SetStatus($"Comparison complete. {successes} results.");
            }
            else if (successes > 0)
            {
                SetStatus($"{successes} succeeded, {failures.Count} failed: {string.Join("; ", failures)}", isError: true);
            }
            else
            {
                SetStatus($"All models failed: {string.Join("; ", failures)}", isError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Comparison failed unexpectedly");
            SetStatus($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>Runs a single model and appends the result to the collection.</summary>
    private async Task RunSingleAsync(ModelConfig? model)
    {
        if (model is null)
        {
            return;
        }

        IsRunning = true;
        SetStatus($"Running {model.DisplayName}...");

        try
        {
            var result = await _orchestrator.RunBriefAsync(model, _promptProvider.GetNewsBriefPrompt());

            if (result.IsSuccess)
            {
                var run = result.Value;
                Results.Add(run);
                SetStatus($"{model.DisplayName} complete — {run.TotalTokens} tokens, {run.Duration.TotalSeconds:F1}s");
            }
            else
            {
                SetStatus($"{model.DisplayName} failed: {result.Errors.First().Message}", isError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Single run failed unexpectedly for {0}", model.DisplayName);
            SetStatus($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            IsRunning = false;
        }
    }
}
