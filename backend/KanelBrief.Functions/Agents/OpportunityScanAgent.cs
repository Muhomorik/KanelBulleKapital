using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// Opportunity Scan agent: identifies actionable rotation opportunities from capital chains.
/// Evaluates rotation paths for investment potential and associated risks.
/// </summary>
public class OpportunityScanAgent
{
    private readonly ILogger<OpportunityScanAgent> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpportunityScanAgent(
        ILogger<OpportunityScanAgent> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
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

            // Create the Opportunity Scan run
            var run = new OpportunityScanRun
            {
                RunDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                DurationSeconds = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                SubstitutionChainRunId = request.SubstitutionChainRunId,
                Targets = GenerateTargets()
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // TODO: Integrate Microsoft Agent Framework
                // - Fetch the substitution chain run by ID
                // - Evaluate each rotation path for opportunity strength
                // - Assess entry points and risk factors
                // - Score and rank opportunities
                _logger.LogInformation("Opportunity Scan completed: {TargetCount} opportunities", run.Targets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed");
                run.Status = RunStatus.Partial;
            }

            run.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

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

    private List<RotationTarget> GenerateTargets()
    {
        // TODO: Replace with Agent Framework LLM analysis of rotation chains
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

/// <summary>Input request for Opportunity Scan agent.</summary>
public class OpportunityScanRequest
{
    public string SubstitutionChainRunId { get; set; } = string.Empty;
}
