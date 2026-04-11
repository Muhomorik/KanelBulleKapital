using System.Text.Json;
using Azure.AI.Projects;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// Substitution Chain agent: identifies capital rotation paths from weekly market themes.
/// Analyzes where capital is flowing from and to based on market sentiment patterns.
/// Integrates with Microsoft Agent Framework for LLM-powered analysis.
/// </summary>
public class SubstitutionChainAgent
{
    private readonly ILogger<SubstitutionChainAgent> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public SubstitutionChainAgent(
        ILogger<SubstitutionChainAgent> logger,
        AIProjectClient aiProjectClient,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    [Function("SubstitutionChain")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "substitution-chain")] HttpRequestData req)
    {
        try
        {
            // Parse input: weekly summary run ID
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<SubstitutionChainRequest>(requestBody ?? string.Empty, _jsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            _logger.LogInformation("Processing substitution chains from weekly summary {RunId}", request.WeeklySummaryRunId);

            var startTime = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString();

            // Create the Substitution Chain run
            var run = new SubstitutionChainRun
            {
                RunDate = startTime.ToString("yyyy-MM-dd"),
                RunId = runId,
                CreatedAt = startTime,
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                DurationSeconds = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                WeeklySummaryRunId = request.WeeklySummaryRunId,
                Chains = []
            };

            try
            {
                // Use Microsoft Agent Framework for analysis
                var analysis = await AnalyzeChainsWithAgentAsync(request.WeeklySummaryRunId);
                run.Chains = analysis.Chains;

                _logger.LogInformation("Substitution Chain analysis completed: {ChainCount} chains", run.Chains.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed, using fallback");
                run.Status = RunStatus.Partial;
                run.Chains = GenerateFallbackChains();
            }

            run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

            // Save to repository
            await _repository.SaveSubstitutionChainRunAsync(run);
            _logger.LogInformation("Substitution Chain run saved: {RunId}", run.RunId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubstitutionChain function failed");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    private async Task<SubstitutionChainAnalysisResult> AnalyzeChainsWithAgentAsync(string weeklySummaryRunId)
    {
        // Create agent for rotation analysis
        var agent = _aiProjectClient.AsAIAgent(
            model: "gpt-5.4-mini",
            name: "SubstitutionChainAnalyzer",
            instructions: @"You are a financial market rotation analyst. Analyze capital rotation patterns and identify substitution chains.
A substitution chain shows where capital is fleeing from and flowing toward based on market sentiment.

Return a JSON object with this exact structure:
{
  ""chains"": [
    {
      ""capitalFleeing"": ""Sector or asset class losing capital"",
      ""flowsToward"": ""Sector or asset class gaining capital"",
      ""mechanism"": ""Why capital is rotating (e.g., ESG-driven, valuation, growth expectations)""
    }
  ]
}"
        );

        var prompt = $"Based on the weekly market summary (ID: {weeklySummaryRunId}), identify capital rotation chains.\nAnalyze sentiment trends to determine which sectors are losing capital and which are gaining it.";

        var agentResponse = await agent.RunAsync(prompt);
        var responseText = agentResponse.ToString() ?? string.Empty;

        var analysisJson = AgentResponseParser.ExtractJson(responseText);
        var analysis = JsonSerializer.Deserialize<SubstitutionChainAnalysisResult>(analysisJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse agent response");

        return analysis;
    }

    internal static List<RotationChain> GenerateFallbackChains()
    {
        return new List<RotationChain>
        {
            new()
            {
                CapitalFleeing = "Energy",
                FlowsToward = "Technology",
                Mechanism = "ESG-driven reallocation and tech sector outperformance"
            }
        };
    }
}
