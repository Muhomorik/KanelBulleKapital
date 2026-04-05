using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Overall mood assessment for a single news brief run.
/// Contains the dominant market sentiment and a collection of per-category assessments.
/// </summary>
[DebuggerDisplay("{Mood}: {Summary}")]
public class NewsItem
{
    private readonly List<CategoryAssessment> _assessments = [];

    public Guid ItemId { get; private set; }
    public MarketSentiment Mood { get; private set; }
    public string Summary { get; private set; }

    /// <summary>FK to the parent <see cref="NewsBriefRun"/>.</summary>
    public Guid RunId { get; private set; }

    /// <summary>Per-category news assessments parsed from the agent's output.</summary>
    public IReadOnlyList<CategoryAssessment> Assessments => _assessments.AsReadOnly();

    private NewsItem() // EF Core
    {
        Summary = null!;
    }

    /// <summary>
    /// Creates a new news item with overall mood and summary.
    /// </summary>
    /// <param name="mood">Overall dominant market sentiment.</param>
    /// <param name="summary">Overall mood summary text.</param>
    public NewsItem(MarketSentiment mood, string summary)
    {
        ItemId = Guid.NewGuid();
        Mood = mood;
        Summary = summary;
    }

    /// <summary>
    /// Adds a per-category assessment to this item.
    /// </summary>
    public void AddAssessment(CategoryAssessment assessment)
    {
        _assessments.Add(assessment);
    }
}
