using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// A single market-moving news event within a specific category.
/// Category is free text from the LLM (e.g., "Geopolitics / Energy", "Central Banks").
/// </summary>
[DebuggerDisplay("{Category}: {Headline} ({Sentiment})")]
public class CategoryAssessment
{
    public Guid AssessmentId { get; private set; }
    public string Category { get; private set; }
    public string Headline { get; private set; }
    public string Summary { get; private set; }
    public MarketSentiment Sentiment { get; private set; }

    /// <summary>FK to the parent <see cref="NewsItem"/>.</summary>
    public Guid ItemId { get; private set; }

    private CategoryAssessment() // EF Core
    {
        Category = null!;
        Headline = null!;
        Summary = null!;
    }

    /// <summary>
    /// Creates a new category assessment parsed from agent output.
    /// </summary>
    /// <param name="category">Free text category name from the LLM.</param>
    /// <param name="headline">One-line bold headline.</param>
    /// <param name="summary">One-sentence market impact summary.</param>
    /// <param name="sentiment">Directional impact flag.</param>
    public CategoryAssessment(string category, string headline, string summary, MarketSentiment sentiment)
    {
        AssessmentId = Guid.NewGuid();
        Category = category;
        Headline = headline;
        Summary = summary;
        Sentiment = sentiment;
    }
}
