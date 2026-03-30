using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.ValueObjects;

/// <summary>
/// Overall market mood summary produced at the end of a news brief.
/// </summary>
/// <param name="DominantSentiment">The prevailing market direction.</param>
/// <param name="MoodSummary">Two-line closing summary from the agent.</param>
[DebuggerDisplay("{DominantSentiment}: {MoodSummary}")]
public sealed record MarketMood(
    MarketSentiment DominantSentiment,
    string MoodSummary);
