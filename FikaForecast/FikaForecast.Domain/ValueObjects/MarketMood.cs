using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.ValueObjects;

/// <summary>
/// Overall market mood summary produced at the end of a news brief.
/// Stored as an EF Core owned type — flattened into the <c>NewsBriefRuns</c> table
/// as <c>Mood_DominantSentiment</c> and <c>Mood_MoodSummary</c> columns.
/// </summary>
/// <param name="DominantSentiment">The prevailing market direction (Risk-off, Risk-on, or Mixed).</param>
/// <param name="MoodSummary">Two-line closing summary from the agent explaining the overall market mood.
/// <example>"Markets likely to stay defensive until June CPI confirms the disinflation trend."</example></param>
[DebuggerDisplay("{DominantSentiment}: {MoodSummary}")]
public sealed record MarketMood(
    MarketSentiment DominantSentiment,
    string MoodSummary);
