using KanelBrief.Core.Models;

namespace KanelBrief.Core.Agents;

/// <summary>
/// Domain port for News Brief analysis. Returns a parsed analysis result
/// — infrastructure concerns (LLM calls, JSON parsing, Azure SDK) live behind the implementation.
/// </summary>
public interface INewsBriefAnalyzer
{
    /// <summary>
    /// Autonomous analysis: the analyzer generates its own market content for the given timestamp
    /// (e.g. via Bing Grounding). Used by the timer-driven daily pipeline.
    /// </summary>
    Task<NewsBriefAnalysisResult> AnalyzeAsync(DateTimeOffset asOf, CancellationToken ct = default);

    /// <summary>
    /// Article-driven analysis: the caller supplies pre-scraped news articles to analyse.
    /// Used by the HTTP-triggered <c>POST /news-brief</c> endpoint.
    /// </summary>
    Task<NewsBriefAnalysisResult> AnalyzeAsync(IReadOnlyList<NewsArticle> articles, CancellationToken ct = default);
}
