using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Projection of <see cref="Domain.Entities.NewsBriefRun"/> for the presentation layer.
/// </summary>
[DebuggerDisplay("{DeploymentName} — {Status} ({TotalTokens} tokens)")]
public sealed record NewsBriefRunDto(
    Guid RunId,
    DateTimeOffset Timestamp,
    string ModelId,
    string DeploymentName,
    string PromptName,
    TimeSpan Duration,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    RunStatus Status,
    string RawMarkdownOutput,
    MarketMoodDto? Mood,
    IReadOnlyList<NewsItemDto> Items);

/// <summary>
/// Projection of <see cref="Domain.Entities.NewsItem"/> for the presentation layer.
/// </summary>
[DebuggerDisplay("{Category}: {Headline}")]
public sealed record NewsItemDto(
    Guid ItemId,
    NewsCategory Category,
    string Headline,
    string Summary,
    MarketSentiment Sentiment);

/// <summary>
/// Projection of <see cref="Domain.ValueObjects.MarketMood"/> for the presentation layer.
/// </summary>
[DebuggerDisplay("{DominantSentiment}")]
public sealed record MarketMoodDto(
    MarketSentiment DominantSentiment,
    string MoodSummary);
