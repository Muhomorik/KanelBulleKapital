using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// Substitution Chain agent: identifies capital rotation paths from weekly market themes.
/// Analyzes where capital is flowing from and to based on market sentiment patterns.
/// </summary>
public class SubstitutionChainAgent
{
    private readonly ILogger<SubstitutionChainAgent> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public SubstitutionChainAgent(
        ILogger<SubstitutionChainAgent> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
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

            // Create the Substitution Chain run
            var run = new SubstitutionChainRun
            {
                RunDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                DurationSeconds = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                WeeklySummaryRunId = request.WeeklySummaryRunId,
                Chains = GenerateChains()
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // TODO: Integrate Microsoft Agent Framework
                // - Fetch the weekly summary run by ID
                // - Analyze theme sentiment transitions
                // - Identify capital rotation paths
                // - Map from declining to rising sectors
                _logger.LogInformation("Substitution Chain analysis completed: {ChainCount} chains", run.Chains.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed");
                run.Status = RunStatus.Partial;
            }

            run.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

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

    private List<RotationChain> GenerateChains()
    {
        // TODO: Replace with Agent Framework LLM analysis of weekly themes
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

/// <summary>Input request for Substitution Chain agent.</summary>
public class SubstitutionChainRequest
{
    public string WeeklySummaryRunId { get; set; } = string.Empty;
}
