using System.Net;
using System.Text.Json;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Repositories;
using KanelBrief.Core.Serialization;
using KanelBrief.Core.Time;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// HTTP coordinator for the Weekly Summary agent. Business logic lives in
/// <see cref="ExecuteAsync"/> for direct unit testing. Loads the week's daily briefs from the
/// repository and delegates analysis to <see cref="IWeeklySummaryAnalyzer"/>.
/// Returns 500 on failure — nothing is persisted.
/// </summary>
public sealed class WeeklySummaryAgent(
    ILogger<WeeklySummaryAgent> logger,
    IWeeklySummaryAnalyzer analyzer,
    IAgentRunRepository repository,
    TimeProvider timeProvider)
{
    private const string ModelId = "gpt-5.4-mini";

    private static readonly JsonSerializerOptions JsonOptions = KanelJsonOptions.CamelCase;

    [Function("WeeklySummary")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "weekly-summary")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<WeeklySummaryRequest>(body ?? string.Empty, JsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            var run = await ExecuteAsync(request, ct);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weekly Summary agent failed");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Business logic: loads the week's daily briefs from the repo, runs them through the
    /// analyzer, persists the resulting weekly summary. Throws if no briefs exist for the week
    /// or if any step fails — caller handles logging/HTTP response.
    /// </summary>
    internal async Task<WeeklySummaryRun> ExecuteAsync(WeeklySummaryRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing weekly summary for period {PeriodStart} → {PeriodEnd}",
            request.PeriodStart, request.PeriodEnd);

        var startTime = timeProvider.GetUtcNow();

        var dailyBriefs = new List<NewsBriefRun>();
        for (var date = request.PeriodStart.UtcDateTime.Date;
             date <= request.PeriodEnd.UtcDateTime.Date;
             date = date.AddDays(1))
        {
            var briefsForDate = await repository.GetNewsBriefRunsByDateAsync(date.ToString("yyyy-MM-dd"));
            dailyBriefs.AddRange(briefsForDate);
        }

        if (dailyBriefs.Count == 0)
            throw new InvalidOperationException(
                $"No daily briefs found for period {request.PeriodStart:yyyy-MM-dd} → {request.PeriodEnd:yyyy-MM-dd}");

        var analysis = await analyzer.AnalyzeAsync(
            request.PeriodStart.UtcDateTime.Date,
            request.PeriodEnd.UtcDateTime.Date,
            dailyBriefs,
            ct);

        var run = new WeeklySummaryRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = Guid.NewGuid().ToString(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            PeriodIsoWeek = IsoWeek.Format(request.PeriodStart),
            NetMood = AgentResponseParser.ParseSentiment(analysis.Mood),
            MoodSummary = analysis.Summary,
            Themes = analysis.Themes,
            DurationSeconds = (timeProvider.GetUtcNow() - startTime).TotalSeconds
        };

        await repository.SaveWeeklySummaryRunAsync(run);
        logger.LogInformation("Weekly Summary run saved: {RunId} ({ThemeCount} themes)",
            run.RunId, run.Themes.Count);
        return run;
    }
}
