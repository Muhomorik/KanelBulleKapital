using System.Collections.ObjectModel;
using System.Windows.Input;

using DevExpress.Mvvm;

using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;

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
    private readonly BriefComparisonService _comparisonService;
    private readonly NewsBriefOrchestrator _orchestrator;

    public ObservableCollection<ModelConfig> AvailableModels { get; } = [];
    public ObservableCollection<ModelConfig> SelectedModels { get; } = [];
    public ObservableCollection<NewsBriefRun> Results { get; } = [];

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

    /// <summary>True when Azure AI Foundry endpoint and API key are configured.</summary>
    public bool IsConfigured { get; }

    /// <summary>User-facing message explaining what configuration is missing.</summary>
    public string? ConfigurationMessage { get; }

    public ICommand RunComparisonCommand { get; }
    public ICommand RunSingleCommand { get; }

    public ComparisonViewModel(
        ILogger logger,
        IConfiguration configuration,
        BriefComparisonService comparisonService,
        NewsBriefOrchestrator orchestrator,
        IEnumerable<ModelConfig> models)
    {
        _logger = logger;
        _comparisonService = comparisonService;
        _orchestrator = orchestrator;

        foreach (var model in models)
        {
            AvailableModels.Add(model);
        }

        // Check configuration
        var (isConfigured, message) = CheckConfiguration(configuration);
        IsConfigured = isConfigured;
        ConfigurationMessage = message;

        if (!isConfigured)
        {
            _logger.Warn("Azure AI Foundry not configured: {0}", message);
        }

        RunComparisonCommand = new AsyncCommand(RunComparisonAsync, () => !IsRunning && SelectedModels.Count >= 2);
        RunSingleCommand = new AsyncCommand<ModelConfig>(RunSingleAsync, _ => !IsRunning);
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

    /// <summary>
    /// Returns the default News Brief prompt.
    /// Will be replaced with config-loaded prompts.
    /// </summary>
    private AgentPrompt GetDefaultNewsBriefPrompt()
    {
        return new AgentPrompt(
            "News Brief - Default",
            """
            You are a sharp financial intelligence analyst. Your job is to scan the last 14 days of global news and extract only what matters for financial markets.

            Every time you run, you will:

            1. Search the web for major news from the past 2 weeks across these categories:
               - Macroeconomics (inflation, GDP, employment data)
               - Central banks (Fed, ECB, BOJ, BOE decisions or signals)
               - Geopolitics (wars, sanctions, trade disputes, elections)
               - Energy & commodities (oil, gas, metals)
               - Tech & AI (major earnings, regulations, breakthroughs)
               - Corporate (major earnings surprises, bankruptcies, M&A)
               - Financial system (credit events, banking stress, currency moves)

            2. For each relevant item, write one sentence max: what happened + why it matters for markets.

            3. Flag the market impact direction: Risk-off / Risk-on / Mixed or unclear

            4. End with a 2-line overall market mood summary.

            Rules:
            - No fluff. No background context. No history lessons.
            - If something has no clear market implication, skip it.
            - Prioritize surprises and changes over expected events.
            - Today's date is {current_date}.
            """);
    }

    /// <summary>Runs all selected models in parallel and adds results to the collection.</summary>
    private async Task RunComparisonAsync()
    {
        IsRunning = true;
        StatusMessage = $"Running comparison across {SelectedModels.Count} models...";
        Results.Clear();

        try
        {
            var runs = await _comparisonService.CompareAsync(
                SelectedModels.ToList(),
                GetDefaultNewsBriefPrompt());

            foreach (var run in runs)
            {
                Results.Add(run);
            }

            StatusMessage = $"Comparison complete. {runs.Count} results.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Comparison failed");
            StatusMessage = $"Error: {ex.Message}";
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
        StatusMessage = $"Running {model.DisplayName}...";

        try
        {
            var run = await _orchestrator.RunBriefAsync(model, GetDefaultNewsBriefPrompt());
            Results.Add(run);
            StatusMessage = $"{model.DisplayName} complete — {run.TotalTokens} tokens, {run.Duration.TotalSeconds:F1}s";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Single run failed for {0}", model.DisplayName);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }
}