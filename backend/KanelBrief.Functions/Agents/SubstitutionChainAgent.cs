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
/// HTTP coordinator for the Substitution Chain agent. Business logic lives in
/// <see cref="ExecuteAsync"/> for direct unit testing. Loads the referenced weekly summary
/// from the repository and delegates analysis to <see cref="ISubstitutionChainAnalyzer"/>.
/// Returns 500 on failure — nothing is persisted.
/// </summary>
public sealed class SubstitutionChainAgent(
    ILogger<SubstitutionChainAgent> logger,
    ISubstitutionChainAnalyzer analyzer,
    IAgentRunRepository repository,
    TimeProvider timeProvider)
{
    private const string ModelId = "gpt-5.4-mini";

    private static readonly JsonSerializerOptions JsonOptions = KanelJsonOptions.CamelCase;

    [Function("SubstitutionChain")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "substitution-chain")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<SubstitutionChainRequest>(body ?? string.Empty, JsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            var run = await ExecuteAsync(request, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Substitution Chain agent failed");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Business logic: loads the referenced weekly summary, runs it through the analyzer,
    /// persists the chain. Throws if the summary isn't found or any step fails.
    /// </summary>
    internal async Task<SubstitutionChainRun> ExecuteAsync(SubstitutionChainRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.WeeklySummaryRunDate)
            || string.IsNullOrWhiteSpace(request.WeeklySummaryRunId))
            throw new ArgumentException("WeeklySummaryRunDate and WeeklySummaryRunId are required", nameof(request));

        logger.LogInformation(
            "Processing substitution chain for weekly summary {RunDate}/{RunId}",
            request.WeeklySummaryRunDate, request.WeeklySummaryRunId);

        var startTime = timeProvider.GetUtcNow();

        var weeklySummary = await repository.GetWeeklySummaryRunAsync(
            request.WeeklySummaryRunDate, request.WeeklySummaryRunId);

        if (weeklySummary is null)
            throw new InvalidOperationException(
                $"Weekly summary not found: {request.WeeklySummaryRunDate}/{request.WeeklySummaryRunId}");

        var analysis = await analyzer.AnalyzeAsync(weeklySummary, ct);

        var run = new SubstitutionChainRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = Guid.NewGuid().ToString(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            WeeklySummaryRunId = weeklySummary.RunId,
            Chains = analysis.Chains,
            DurationSeconds = (timeProvider.GetUtcNow() - startTime).TotalSeconds
        };

        await repository.SaveSubstitutionChainRunAsync(run);
        logger.LogInformation("Substitution Chain run saved: {RunId} ({ChainCount} chains)",
            run.RunId, run.Chains.Count);
        return run;
    }
}
