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
/// HTTP coordinator for the News Brief agent. The <see cref="RunAsync"/> method is the Function
/// entry point and handles only HTTP parsing/writing. All business logic lives in
/// <see cref="ExecuteAsync"/>, which takes domain inputs and is directly unit-testable.
/// On failure the exception is logged (captured by App Insights) — nothing is persisted.
/// </summary>
public sealed class NewsBriefAgent(
    ILogger<NewsBriefAgent> logger,
    INewsBriefAnalyzer analyzer,
    IAgentRunRepository repository,
    TimeProvider timeProvider)
{
    private const string ModelId = "gpt-5.4-mini";

    private static readonly JsonSerializerOptions JsonOptions = KanelJsonOptions.CamelCase;

    [Function("NewsBrief")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "news-brief")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var articles = JsonSerializer.Deserialize<List<NewsArticle>>(body ?? string.Empty, JsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            var run = await ExecuteAsync(articles, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId.Value, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "News Brief agent failed");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Business logic for the News Brief HTTP agent. Takes the parsed article list,
    /// invokes the analyzer, persists the run, and returns the saved entity.
    /// Throws on any failure (caller is responsible for logging and HTTP conversion).
    /// </summary>
    internal async Task<NewsBriefRun> ExecuteAsync(IReadOnlyList<NewsArticle> articles, CancellationToken ct = default)
    {
        if (articles.Count == 0)
            throw new ArgumentException("At least one news article is required", nameof(articles));

        logger.LogInformation("Processing {ArticleCount} news articles", articles.Count);

        var startTime = timeProvider.GetUtcNow();
        var analysis = await analyzer.AnalyzeAsync(articles, ct);

        var run = new NewsBriefRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = NewsBriefRunId.NewId(),
            CreatedAt = startTime,
            ModelId = ModelId,
            DeploymentName = ModelId,
            Status = RunStatus.Success,
            Mood = analysis.Mood,
            Summary = analysis.Summary,
            Assessments = analysis.Assessments,
            Citations = analysis.Citations,
            DurationSeconds = (timeProvider.GetUtcNow() - startTime).TotalSeconds
        };

        await repository.SaveNewsBriefRunAsync(run);
        logger.LogInformation("News Brief run saved: {RunId} ({AssessmentCount} assessments)",
            run.RunId, run.Assessments.Count);
        return run;
    }
}
