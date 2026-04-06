using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents;

/// <summary>
/// News Brief agent: analyzes daily news and produces market sentiment briefing.
/// Accepts news articles as input and produces structured market sentiment assessment.
/// </summary>
public class NewsBriefAgent
{
    private readonly IAgentRunRepository _repository;
    private readonly ILogger<NewsBriefAgent> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public NewsBriefAgent(
        ILogger<NewsBriefAgent> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
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

            // Create the News Brief run
            var run = new NewsBriefRun
            {
                RunDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                DurationSeconds = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                DeploymentName = "gpt-5.4-mini",
                Mood = "Mixed",
                Summary = GenerateSummary(newsArticles),
                Assessments = GenerateAssessments(newsArticles)
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // TODO: Integrate Microsoft Agent Framework for LLM analysis
                // For now, generate assessments from input structure
                _logger.LogInformation("News Brief analysis completed: {Mood}", run.Mood);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis processing failed");
                run.Status = RunStatus.Partial;
            }

            run.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

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

    private string GenerateSummary(List<NewsArticle> articles)
    {
        // TODO: Replace with Agent Framework LLM call
        var categories = articles.Select(a => a.Category).Distinct();
        return $"Market briefing covering {categories.Count()} sectors: {string.Join(", ", categories)}.";
    }

    private List<CategoryAssessment> GenerateAssessments(List<NewsArticle> articles)
    {
        // TODO: Replace with Agent Framework LLM call
        // For now, create basic assessments from input
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

/// <summary>Input model for news articles.</summary>
public class NewsArticle
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
