using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// Weekly Summary agent: aggregates daily news briefs into weekly themes and market mood.
/// Consumes NewsBriefRuns from a week to identify patterns and sentiment trends.
/// </summary>
public class WeeklySummaryAgent
{
    private readonly ILogger<WeeklySummaryAgent> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public WeeklySummaryAgent(
        ILogger<WeeklySummaryAgent> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    [Function("WeeklySummary")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "weekly-summary")] HttpRequestData req)
    {
        try
        {
            // Parse input: week parameters and daily brief run IDs
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<WeeklySummaryRequest>(requestBody ?? string.Empty, _jsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            _logger.LogInformation("Processing weekly summary for week starting {WeekStart}", request.WeekStart);

            // Create the Weekly Summary run
            var run = new WeeklySummaryRun
            {
                RunDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                DurationSeconds = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                WeekStart = request.WeekStart,
                WeekEnd = request.WeekEnd,
                NetMood = MarketSentiment.Mixed,
                MoodSummary = "Weekly market assessment derived from daily briefs",
                Themes = GenerateThemes()
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // TODO: Integrate Microsoft Agent Framework
                // - Fetch daily briefs by run IDs
                // - Aggregate sentiment and categories
                // - Extract recurring themes
                // - Determine net mood
                _logger.LogInformation("Weekly Summary analysis completed: {Mood}", run.NetMood);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed");
                run.Status = RunStatus.Partial;
            }

            run.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

            // Save to repository
            await _repository.SaveWeeklySummaryRunAsync(run);
            _logger.LogInformation("Weekly Summary run saved: {RunId}", run.RunId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeeklySummary function failed");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    private List<WeeklySummaryTheme> GenerateThemes()
    {
        // TODO: Replace with Agent Framework LLM analysis of daily briefs
        return new List<WeeklySummaryTheme>
        {
            new()
            {
                Category = "Technology",
                Summary = "Tech sector showing resilience with cloud spending growth",
                Confidence = ConfidenceLevel.High,
                Sentiment = MarketSentiment.RiskOn
            }
        };
    }
}

/// <summary>Input request for Weekly Summary agent.</summary>
public class WeeklySummaryRequest
{
    public DateTimeOffset WeekStart { get; set; }
    public DateTimeOffset WeekEnd { get; set; }
    public List<string> DailyBriefRunIds { get; set; } = [];
}
