using System.Text.Json;
using Azure.AI.Projects;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// Opportunity Scan agent: identifies actionable rotation opportunities from capital chains.
/// Evaluates rotation paths for investment potential and associated risks.
/// Integrates with Microsoft Agent Framework for LLM-powered analysis.
/// </summary>
public class OpportunityScanAgent
{
    private readonly ILogger<OpportunityScanAgent> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpportunityScanAgent(
        ILogger<OpportunityScanAgent> logger,
        AIProjectClient aiProjectClient,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    [Function("OpportunityScan")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "opportunity-scan")] HttpRequestData req)
    {
        try
        {
            // Parse input: substitution chain run ID
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<OpportunityScanRequest>(requestBody ?? string.Empty, _jsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            _logger.LogInformation("Processing opportunities from substitution chain {RunId}", request.SubstitutionChainRunId);

            var startTime = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString();

            // Create the Opportunity Scan run
            var run = new OpportunityScanRun
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
                SubstitutionChainRunId = request.SubstitutionChainRunId,
                Targets = []
            };

            try
            {
                // Use Microsoft Agent Framework for analysis
                var analysis = await AnalyzeOpportunitiesWithAgentAsync(request.SubstitutionChainRunId);
                run.Targets = analysis.Targets;

                _logger.LogInformation("Opportunity Scan completed: {TargetCount} opportunities", run.Targets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed, using fallback");
                run.Status = RunStatus.Partial;
                run.Targets = GenerateFallbackTargets();
            }

            run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

            // Save to repository
            await _repository.SaveOpportunityScanRunAsync(run);
            _logger.LogInformation("Opportunity Scan run saved: {RunId}", run.RunId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpportunityScan function failed");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    private async Task<OpportunityScanAnalysisResult> AnalyzeOpportunitiesWithAgentAsync(string substitutionChainRunId)
    {
        // Create agent for opportunity analysis
        var agent = _aiProjectClient.AsAIAgent(
            model: "gpt-5.4-mini",
            name: "OpportunityScanAnalyzer",
            instructions: @"You are a financial investment analyst. Evaluate capital rotation opportunities and identify actionable targets.

Return a JSON object with this exact structure:
{
  ""targets"": [
    {
      ""category"": ""Asset or sector to invest in"",
      ""signalStrength"": ""Strong|Medium|Weak"",
      ""rationale"": ""Why this is a good opportunity"",
      ""riskCaveat"": ""Key risks or conditions to watch""
    }
  ]
}"
        );

        var prompt = $"Based on the substitution chain analysis (ID: {substitutionChainRunId}), identify investment opportunities.\nEvaluate each rotation path for signal strength, entry points, and risks.";

        var agentResponse = await agent.RunAsync(prompt);
        var responseText = agentResponse.ToString() ?? string.Empty;

        var analysisJson = ExtractJson(responseText);
        var analysis = JsonSerializer.Deserialize<OpportunityScanAnalysisResult>(analysisJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse agent response");

        return analysis;
    }

    private string ExtractJson(string text)
    {
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("No JSON found in agent response");

        return text[startIndex..(endIndex + 1)];
    }

    private SignalStrength ParseSignalStrength(string strength)
    {
        return strength.ToLowerInvariant() switch
        {
            "strong" => SignalStrength.Strong,
            "moderate" => SignalStrength.Moderate,
            "weak" => SignalStrength.Weak,
            _ => SignalStrength.Moderate
        };
    }

    private List<RotationTarget> GenerateFallbackTargets()
    {
        return new List<RotationTarget>
        {
            new()
            {
                Category = "Technology - Cloud Infrastructure",
                SignalStrength = SignalStrength.Strong,
                Rationale = "Sustained capital inflow from energy sector reallocation",
                RiskCaveat = "Valuation at historical highs; watch for sentiment reversal"
            }
        };
    }
}
