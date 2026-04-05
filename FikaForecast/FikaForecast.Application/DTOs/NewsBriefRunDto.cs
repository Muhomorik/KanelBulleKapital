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
    NewsItemDto? Item);

/// <summary>
/// Projection of <see cref="Domain.Entities.NewsItem"/> for the presentation layer.
/// </summary>
[DebuggerDisplay("{Mood}: {Summary}")]
public sealed record NewsItemDto(
    Guid ItemId,
    MarketSentiment Mood,
    string Summary,
    IReadOnlyList<CategoryAssessmentDto> Assessments);

/// <summary>
/// Projection of <see cref="Domain.Entities.CategoryAssessment"/> for the presentation layer.
/// </summary>
[DebuggerDisplay("{Category}: {Headline}")]
public sealed record CategoryAssessmentDto(
    Guid AssessmentId,
    string Category,
    string Headline,
    string Summary,
    MarketSentiment Sentiment);
