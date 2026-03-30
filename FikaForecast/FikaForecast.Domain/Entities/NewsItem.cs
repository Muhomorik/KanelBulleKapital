using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// A single market-moving news event extracted from a brief.
/// </summary>
[DebuggerDisplay("{Category}: {Headline} ({Sentiment})")]
public class NewsItem
{
    public Guid ItemId { get; private set; }
    public NewsCategory Category { get; private set; }
    public string Headline { get; private set; }
    public string Summary { get; private set; }
    public MarketSentiment Sentiment { get; private set; }

    /// <summary>FK to the parent <see cref="NewsBriefRun"/>.</summary>
    public Guid RunId { get; private set; }

    private NewsItem() // EF Core
    {
        Headline = null!;
        Summary = null!;
    }

    /// <summary>
    /// Creates a new news item parsed from agent output.
    /// </summary>
    /// <param name="category">Topic classification.</param>
    /// <param name="headline">One-line bold headline.</param>
    /// <param name="summary">One-sentence market impact summary.</param>
    /// <param name="sentiment">Directional impact flag.</param>
    public NewsItem(NewsCategory category, string headline, string summary, MarketSentiment sentiment)
    {
        ItemId = Guid.NewGuid();
        Category = category;
        Headline = headline;
        Summary = summary;
        Sentiment = sentiment;
    }
}
