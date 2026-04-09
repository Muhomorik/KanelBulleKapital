using System.Text.Json;
using Azure.AI.Projects;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// News Brief agent: analyzes daily news and produces market sentiment briefing.
/// Accepts news articles as input and produces structured market sentiment assessment.
/// Integrates with Microsoft Agent Framework for LLM-powered analysis.
/// </summary>
public class NewsBriefAgent
{
    private readonly ILogger<NewsBriefAgent> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public NewsBriefAgent(
        ILogger<NewsBriefAgent> logger,
        AIProjectClient aiProjectClient,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    [Function("NewsBrief")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "news-brief")] HttpRequestData req)
    {
        try
        {
            // Parse input: list of news articles
            var requestBody = await req.ReadAsStringAsync();
            var newsArticles = JsonSerializer.Deserialize<List<NewsArticle>>(requestBody ?? string.Empty, _jsonOptions)
                ?? throw new ArgumentException("Invalid request body");

            if (newsArticles.Count == 0)
                throw new ArgumentException("At least one news article is required");

            _logger.LogInformation("Processing {ArticleCount} news articles", newsArticles.Count);

            var startTime = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString();

            // Create the News Brief run
            var run = new NewsBriefRun
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
                DeploymentName = "gpt-5.4-mini",
                Mood = MarketSentiment.Mixed.ToString(),
                Summary = string.Empty,
                Assessments = []
            };

            try
            {
                // Use Microsoft Agent Framework for analysis
                var analysis = await AnalyzeNewsWithAgentAsync(newsArticles);
                run.Mood = analysis.Mood;
                run.Summary = analysis.Summary;
                run.Assessments = analysis.Assessments;

                _logger.LogInformation("News Brief analysis completed: Mood={Mood}, Assessments={Count}",
                    run.Mood, run.Assessments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed, using fallback");
                run.Status = RunStatus.Partial;
                // Use fallback assessment from article structure
                run.Summary = GenerateFallbackSummary(newsArticles);
                run.Assessments = GenerateFallbackAssessments(newsArticles);
            }

            run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

            // Save to repository
            await _repository.SaveNewsBriefRunAsync(run);
            _logger.LogInformation("News Brief run saved: {RunId}", run.RunId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { runId = run.RunId, status = run.Status });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NewsBrief function failed");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    private async Task<NewsBriefAnalysisResult> AnalyzeNewsWithAgentAsync(List<NewsArticle> articles)
    {
        // Create agent for news analysis
        var agent = _aiProjectClient.AsAIAgent(
            model: "gpt-5.4-mini",
            name: "NewsBriefAnalyzer",
            instructions: @"You are a financial market analyst. Analyze the provided news articles and:
1. Determine the overall market mood (RiskOn, RiskOff, or Mixed)
2. Generate a brief market summary (1-2 sentences)
3. For each category/sector, provide sentiment assessment

Return a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Your analysis summary"",
  ""assessments"": [
    {
      ""category"": ""Sector name"",
      ""headline"": ""Key headline"",
      ""summary"": ""Analysis summary"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}"
        );

        // Format articles for the agent
        var articlesText = string.Join("\n\n", articles.Select((a, i) =>
            $"[Article {i + 1}]\nCategory: {a.Category}\nTitle: {a.Title}\nContent: {a.Content}"));

        var prompt = $"Analyze these market news articles:\n\n{articlesText}";

        // Run agent and get response
        var agentResponse = await agent.RunAsync(prompt);
        var responseText = agentResponse.ToString() ?? string.Empty;

        // Parse JSON response
        var analysisJson = ExtractJson(responseText);
        var analysis = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(analysisJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse agent response");

        return analysis;
    }

    private string ExtractJson(string text)
    {
        // Extract JSON from response (agent might include extra text)
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("No JSON found in agent response");

        return text[startIndex..(endIndex + 1)];
    }

    private string GenerateFallbackSummary(List<NewsArticle> articles)
    {
        var categories = articles.Select(a => a.Category).Distinct();
        return $"Market briefing covering {categories.Count()} sectors: {string.Join(", ", categories)}.";
    }

    private List<CategoryAssessment> GenerateFallbackAssessments(List<NewsArticle> articles)
    {
        return articles
            .GroupBy(a => a.Category)
            .Select(g => new CategoryAssessment
            {
                Category = g.Key,
                Headline = g.First().Title,
                Summary = g.First().Content[..Math.Min(100, g.First().Content.Length)],
                Sentiment = MarketSentiment.Mixed
            })
            .ToList();
    }
}
