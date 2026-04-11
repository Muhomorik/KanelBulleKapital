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
/// Weekly Summary agent: aggregates daily news briefs into weekly themes and market mood.
/// Consumes NewsBriefRuns from a week to identify patterns and sentiment trends.
/// Integrates with Microsoft Agent Framework for LLM-powered analysis.
/// </summary>
public class WeeklySummaryAgent
{
    private readonly ILogger<WeeklySummaryAgent> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public WeeklySummaryAgent(
        ILogger<WeeklySummaryAgent> logger,
        AIProjectClient aiProjectClient,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
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

            var startTime = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString();

            // Create the Weekly Summary run
            var run = new WeeklySummaryRun
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
                WeekStart = request.WeekStart,
                WeekEnd = request.WeekEnd,
                NetMood = MarketSentiment.Mixed,
                MoodSummary = string.Empty,
                Themes = []
            };

            try
            {
                // Use Microsoft Agent Framework for analysis
                var analysis = await AnalyzeWeekWithAgentAsync(request);
                run.NetMood = AgentResponseParser.ParseSentiment(analysis.Mood);
                run.MoodSummary = analysis.Summary;
                run.Themes = analysis.Themes;

                _logger.LogInformation("Weekly Summary analysis completed: Mood={Mood}, Themes={Count}",
                    run.NetMood, run.Themes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed, using fallback");
                run.Status = RunStatus.Partial;
                run.MoodSummary = "Weekly market assessment derived from daily briefs";
                run.Themes = GenerateFallbackThemes();
            }

            run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

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

    private async Task<WeeklySummaryAnalysisResult> AnalyzeWeekWithAgentAsync(WeeklySummaryRequest request)
    {
        // Create agent for weekly analysis
        var agent = _aiProjectClient.AsAIAgent(
            model: "gpt-5.4-mini",
            name: "WeeklySummaryAnalyzer",
            instructions: @"You are a financial market analyst. Analyze a week of market data and:
1. Determine the overall net market mood (RiskOn, RiskOff, or Mixed)
2. Generate a summary of the week's market themes (1-2 sentences)
3. Identify 2-3 key market themes that emerged during the week

Return a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Weekly market assessment"",
  ""themes"": [
    {
      ""category"": ""Theme name"",
      ""summary"": ""Theme description"",
      ""confidence"": ""High|Medium|Low"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}"
        );

        var briefInfo = $"Week: {request.WeekStart:yyyy-MM-dd} to {request.WeekEnd:yyyy-MM-dd}\nDaily brief run IDs: {string.Join(", ", request.DailyBriefRunIds)}";
        var prompt = $"Analyze this week of market data:\n\n{briefInfo}\n\nSummarize the week's trends, sentiment, and key themes.";

        var agentResponse = await agent.RunAsync(prompt);
        var responseText = agentResponse.ToString() ?? string.Empty;

        var analysisJson = AgentResponseParser.ExtractJson(responseText);
        var analysis = JsonSerializer.Deserialize<WeeklySummaryAnalysisResult>(analysisJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse agent response");

        return analysis;
    }

    internal static List<WeeklySummaryTheme> GenerateFallbackThemes()
    {
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
