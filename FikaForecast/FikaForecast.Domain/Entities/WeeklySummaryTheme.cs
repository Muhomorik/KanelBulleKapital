using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// A single consolidated theme from a weekly summary, with confidence level
/// based on persistence across daily briefs.
/// </summary>
[DebuggerDisplay("{Category}: {Confidence} ({Sentiment})")]
public class WeeklySummaryTheme
{
    public Guid ThemeId { get; private set; }
    public string Category { get; private set; }
    public string Summary { get; private set; }
    public ConfidenceLevel Confidence { get; private set; }
    public MarketSentiment Sentiment { get; private set; }

    /// <summary>FK to the parent <see cref="WeeklySummaryRun"/>.</summary>
    public Guid RunId { get; private set; }

    private WeeklySummaryTheme() // EF Core
    {
        Category = null!;
        Summary = null!;
    }

    /// <summary>
    /// Creates a new weekly summary theme parsed from agent output.
    /// </summary>
    public WeeklySummaryTheme(string category, string summary, ConfidenceLevel confidence, MarketSentiment sentiment)
    {
        ThemeId = Guid.NewGuid();
        Category = category;
        Summary = summary;
        Confidence = confidence;
        Sentiment = sentiment;
    }
}
