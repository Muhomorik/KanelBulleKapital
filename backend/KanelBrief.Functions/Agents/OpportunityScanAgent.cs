using System.Net;
using System.Text.Json;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// HTTP coordinator for the Opportunity Scan agent. Business logic lives in
/// <see cref="ExecuteAsync"/> for direct unit testing. Loads the referenced substitution chain
/// from the repository and delegates analysis to <see cref="IOpportunityScanAnalyzer"/>.
/// Returns 500 on failure — nothing is persisted.
/// </summary>
public sealed class OpportunityScanAgent(
    ILogger<OpportunityScanAgent> logger,
    IOpportunityScanAnalyzer analyzer,
    IAgentRunRepository repository,
    TimeProvider timeProvider)
{
    private const string ModelId = "gpt-5.4-mini";

    private static readonly JsonSerializerOptions JsonOptions = KanelJsonOptions.CamelCase;

    [Function("OpportunityScan")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "opportunity-scan")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<OpportunityScanRequest>(body ?? string.Empty, JsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            var run = await ExecuteAsync(request, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId.Value, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Opportunity Scan agent failed");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Business logic: loads the referenced substitution chain, runs it through the analyzer,
    /// persists the opportunity scan. Throws if the chain isn't found or any step fails.
    /// </summary>
    internal async Task<OpportunityScanRun> ExecuteAsync(OpportunityScanRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.SubstitutionChainRunDate)
            || string.IsNullOrWhiteSpace(request.SubstitutionChainRunId.Value))
            throw new ArgumentException("SubstitutionChainRunDate and SubstitutionChainRunId are required", nameof(request));

        logger.LogInformation(
            "Processing opportunity scan for substitution chain {RunDate}/{RunId}",
            request.SubstitutionChainRunDate, request.SubstitutionChainRunId);

        var startTime = timeProvider.GetUtcNow();

        var substitutionChain = await repository.GetSubstitutionChainRunAsync(
            request.SubstitutionChainRunDate, request.SubstitutionChainRunId);

        if (substitutionChain is null)
            throw new InvalidOperationException(
                $"Substitution chain not found: {request.SubstitutionChainRunDate}/{request.SubstitutionChainRunId}");

        var analysis = await analyzer.AnalyzeAsync(substitutionChain, ct);

        var run = new OpportunityScanRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = OpportunityScanRunId.NewId(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            SubstitutionChainRunId = substitutionChain.RunId,
            Targets = analysis.Targets,
            DurationSeconds = (timeProvider.GetUtcNow() - startTime).TotalSeconds
        };

        await repository.SaveOpportunityScanRunAsync(run);
        logger.LogInformation("Opportunity Scan run saved: {RunId} ({TargetCount} targets)",
            run.RunId, run.Targets.Count);
        return run;
    }
}
