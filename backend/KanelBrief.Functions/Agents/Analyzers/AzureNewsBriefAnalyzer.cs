using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Serialization;
using KanelBrief.Functions.Orchestration;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents.Analyzers;

/// <summary>
/// Azure AI Foundry implementation of <see cref="INewsBriefAnalyzer"/>.
/// Creates an ephemeral declarative agent version (optionally with Bing Grounding),
/// runs it via <see cref="ProjectResponsesClient"/>, and deletes the version afterwards.
/// </summary>
public sealed class AzureNewsBriefAnalyzer : INewsBriefAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";

    private readonly ILogger<AzureNewsBriefAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly AgentAdministrationClient _agentAdmin;
    private readonly OrchestratorOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureNewsBriefAnalyzer(
        ILogger<AzureNewsBriefAnalyzer> logger,
        AIProjectClient aiProjectClient,
        AgentAdministrationClient agentAdmin,
        OrchestratorOptions options)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _agentAdmin = agentAdmin;
        _options = options;
        _jsonOptions = KanelJsonOptions.CamelCase;
    }

    public async Task<NewsBriefAnalysisResult> AnalyzeAsync(DateTimeOffset asOf, CancellationToken ct = default)
    {
        var agentDefinition = new DeclarativeAgentDefinition(model: ModelId)
        {
            Instructions = @"You are a financial market analyst. Analyze today's global financial market conditions and produce a morning market brief.

Cover these sectors: Technology, Energy, Financials, Healthcare, Consumer Discretionary, Industrials.
Focus on the most significant market-moving events and trends.

1. Determine the overall market mood (RiskOn, RiskOff, or Mixed)
2. Write a brief 1-2 sentence market summary
3. For each significant sector (at least 3-4), provide a sentiment assessment

Return ONLY a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Your market summary"",
  ""assessments"": [
    {
      ""category"": ""Sector name"",
      ""headline"": ""Key development"",
      ""summary"": ""Brief analysis"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}"
        };

        if (!string.IsNullOrEmpty(_options.BingConnectionName))
        {
            try
            {
                var bingConnection = await _aiProjectClient.Connections
                    .GetConnectionAsync(connectionName: _options.BingConnectionName);
                var bingTool = new BingGroundingTool(
                    new BingGroundingSearchToolOptions(
                        searchConfigurations: [new BingGroundingSearchConfiguration(
                            projectConnectionId: bingConnection.Value.Id)]));
                agentDefinition.Tools.Add(bingTool);
                _logger.LogInformation("Bing Grounding tool enabled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve Bing connection '{Name}' — continuing without Bing Grounding",
                    _options.BingConnectionName);
            }
        }
        else
        {
            _logger.LogWarning("Bing Grounding not configured — agent will use training data only");
        }

        var agentVersionResult = await _agentAdmin.CreateAgentVersionAsync(
            agentName: "kanelbrief-news-brief",
            options: new(agentDefinition));
        var agentVersionValue = agentVersionResult.Value;

        try
        {
            var responsesClient = new ProjectResponsesClient(
                _options.FoundryEndpoint,
                _options.Credential,
                new AgentReference(agentVersionValue.Name, agentVersionValue.Version));

            var userMessage = $"Produce today's morning market brief for {asOf:yyyy-MM-dd}. Analyze the most significant global financial market developments and sector-level sentiment.";

            var clientResult = await responsesClient.CreateResponseAsync(userMessage);
            var responseText = clientResult.Value.GetOutputText();

            var analysisJson = AgentResponseParser.ExtractJson(responseText);
            return JsonSerializer.Deserialize<NewsBriefAnalysisResult>(analysisJson, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse agent response");
        }
        finally
        {
            await _agentAdmin.DeleteAgentVersionAsync(
                agentName: agentVersionValue.Name, agentVersion: agentVersionValue.Version);
        }
    }

    public async Task<NewsBriefAnalysisResult> AnalyzeAsync(
        IReadOnlyList<NewsArticle> articles,
        CancellationToken ct = default)
    {
        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "NewsBriefAnalyzer",
            instructions: @"You are a financial market analyst. Analyze the provided news articles and:
1. Determine the overall market mood (RiskOn, RiskOff, or Mixed)
2. Generate a brief market summary (1-2 sentences)
3. For each category/sector, provide sentiment assessment

Return ONLY a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Your analysis summary"",
  ""assessments"": [
    {
      ""category"": ""Sector name"",
      ""headline"": ""Key headline"",
      ""summary"": ""Analysis summary"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}");

        var articlesText = string.Join("\n\n", articles.Select((a, i) =>
            $"[Article {i + 1}]\nCategory: {a.Category}\nTitle: {a.Title}\nContent: {a.Content}"));
        var prompt = $"Analyze these market news articles:\n\n{articlesText}";

        var response = await agent.RunAsync(prompt);
        var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
        return JsonSerializer.Deserialize<NewsBriefAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse article-based news brief response");
    }
}
