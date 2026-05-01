using System.ClientModel;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Serialization;
using KanelBrief.Functions.Infrastructure;
using KanelBrief.Functions.Orchestration;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents.Analyzers;

/// <summary>
/// Azure AI Foundry implementation of <see cref="INewsBriefAnalyzer"/>.
/// Invokes the persistent Foundry agent <c>kanelbrief-news-brief</c>, which is created
/// once in the Foundry portal (see <c>docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup</c>).
/// Portal-resident agent enables Foundry continuous evaluation (Groundedness + Custom
/// Evaluator) to attach to a stable agent name across runs.
/// </summary>
public sealed class AzureNewsBriefAnalyzer : INewsBriefAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";
    private const int OutputTokenCap = 2000;

    private readonly ILogger<AzureNewsBriefAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly AgentAdministrationClient _agentAdmin;
    private readonly OrchestratorOptions _options;
    private readonly IPromptProvider _promptProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureNewsBriefAnalyzer(
        ILogger<AzureNewsBriefAnalyzer> logger,
        AIProjectClient aiProjectClient,
        AgentAdministrationClient agentAdmin,
        OrchestratorOptions options,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _agentAdmin = agentAdmin;
        _options = options;
        _promptProvider = promptProvider;
        _jsonOptions = KanelJsonOptions.CamelCase;
    }

    public async Task<NewsBriefAnalysisResult> AnalyzeAsync(DateTimeOffset asOf, CancellationToken ct = default)
    {
        // Persistent-agent pattern (see docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup).
        //
        // Instructions, model, and Bing Grounding tool all live in the Foundry portal —
        // not in this code. Why:
        //
        // - Foundry continuous evaluation (Groundedness, Custom Evaluator) attaches to a
        //   stable named agent and scores every run automatically. An ephemeral
        //   create-and-delete-per-run pattern (our old behavior) left no agent in the
        //   portal for evaluators to target.
        // - At a 4-hour run cadence that pattern would pile up ~2,200 agent versions/year.
        //
        // Disaster recovery: the exact instructions and tool config are documented
        // verbatim in docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup so the agent
        // can be recreated if accidentally deleted from the portal.
        const string agentName = "kanelbrief-news-brief";

        ProjectsAgentRecord agentRecord;
        try
        {
            agentRecord = await _agentAdmin.GetAgentAsync(agentName, ct);
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            // Most common setup error: agent hasn't been created in the portal yet.
            // Log with enough detail that the operator can fix it without reading code.
            _logger.LogError(ex,
                "Persistent agent '{AgentName}' not found in Foundry project. " +
                "Create it in the Foundry portal (Build → Agents → + New agent) with the " +
                "instructions and Bing Grounding tool from docs/AZURE-DEPLOYMENT.md § " +
                "Persistent Agent Setup (KanelBrief News Brief). The timer will not " +
                "produce briefs until the agent exists.",
                agentName);
            throw new InvalidOperationException(
                $"Persistent agent '{agentName}' not found in Foundry. " +
                "See docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup (KanelBrief News Brief) " +
                "for one-time portal setup instructions.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve persistent agent '{AgentName}' from Foundry — " +
                "check FOUNDRY_PROJECT_ENDPOINT, Managed Identity RBAC (Azure AI Developer + " +
                "Cognitive Services User), and network reachability.",
                agentName);
            throw;
        }

        _logger.LogInformation(
            "Retrieved persistent agent '{AgentName}' (id: {AgentId}) — latest version resolved by Foundry",
            agentName, agentRecord.Id);

        var agent = _aiProjectClient.AsAIAgent(agentRecord);

        var userMessage = $"Produce the market brief for {asOf:yyyy-MM-dd}.";

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = OutputTokenCap });
        var response = await agent.RunAsync(userMessage, options: runOptions, cancellationToken: ct);

        var responseText = response.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogError(
                "Persistent agent '{AgentName}' returned empty response — possible Bing " +
                "Grounding or model-deployment issue. Check the agent's tool configuration " +
                "in the Foundry portal.",
                agentName);
            throw new InvalidOperationException($"Agent '{agentName}' returned empty response");
        }

        var analysisJson = AgentResponseParser.ExtractJson(responseText);
        var result = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(analysisJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse agent response");

#pragma warning disable OPENAI001
        result.Citations = CitationExtractor.Extract(response);
#pragma warning restore OPENAI001

        var usage = response.Usage;
        if (usage is not null)
        {
            result.InputTokens = (int)(usage.InputTokenCount ?? 0);
            result.OutputTokens = (int)(usage.OutputTokenCount ?? 0);
            result.TotalTokens = (int)(usage.TotalTokenCount ?? 0);
        }
        return result;
    }

    public async Task<NewsBriefAnalysisResult> AnalyzeAsync(
        IReadOnlyList<NewsArticle> articles,
        CancellationToken ct = default)
    {
        var promptDef = _promptProvider.GetNewsBriefArticlesPrompt();
        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "NewsBriefAnalyzer",
            instructions: promptDef.SystemPrompt);

        var articlesText = string.Join("\n\n", articles.Select((a, i) =>
            $"[Article {i + 1}]\nCategory: {a.Category}\nTitle: {a.Title}\nContent: {a.Content}"));
        var prompt = $"Analyze these market news articles:\n\n{articlesText}";

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = OutputTokenCap });
        var response = await agent.RunAsync(prompt, options: runOptions);
        var json = AgentResponseParser.ExtractJson(response.Text ?? string.Empty);
        var result = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse article-based news brief response");

        var usage = response.Usage;
        if (usage is not null)
        {
            result.InputTokens = (int)(usage.InputTokenCount ?? 0);
            result.OutputTokens = (int)(usage.OutputTokenCount ?? 0);
            result.TotalTokens = (int)(usage.TotalTokenCount ?? 0);
        }
        return result;
    }
}
