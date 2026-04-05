using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the Weekly Summary Agent.
/// Consolidates daily briefs into confidence-weighted themes.
/// </summary>
[DebuggerDisplay("Weekly {WeekStart:yyyy-MM-dd}..{WeekEnd:yyyy-MM-dd} — {Status} ({TotalTokens} tokens)")]
public class WeeklySummaryRun
{
    private readonly List<WeeklySummaryTheme> _themes = [];

    public Guid RunId { get; private set; }
    public DateTimeOffset WeekStart { get; private set; }
    public DateTimeOffset WeekEnd { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string ModelId { get; private set; }
    public RunStatus Status { get; private set; }
    public TimeSpan Duration { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }

    /// <summary>Raw JSON response from the agent. Used for audit.</summary>
    public string RawAgentOutput { get; private set; }

    /// <summary>Rendered display markdown with emojis, generated from structured data.</summary>
    public string RawMarkdownOutput { get; private set; }

    /// <summary>Net weekly mood: dominant sentiment across the week.</summary>
    public MarketSentiment NetMood { get; private set; }

    /// <summary>One-line net weekly mood summary.</summary>
    public string MoodSummary { get; private set; }

    /// <summary>Consolidated themes with confidence levels.</summary>
    public IReadOnlyList<WeeklySummaryTheme> Themes => _themes.AsReadOnly();

    private WeeklySummaryRun() // EF Core
    {
        ModelId = null!;
        RawAgentOutput = null!;
        RawMarkdownOutput = null!;
        MoodSummary = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// </summary>
    public static WeeklySummaryRun Start(ModelConfig model, DateTimeOffset weekStart, DateTimeOffset weekEnd)
    {
        return new WeeklySummaryRun
        {
            RunId = Guid.NewGuid(),
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Timestamp = DateTimeOffset.Now,
            ModelId = model.ModelId,
            Status = RunStatus.Partial,
            RawAgentOutput = string.Empty,
            RawMarkdownOutput = string.Empty,
            MoodSummary = string.Empty
        };
    }

    /// <summary>
    /// Marks the run as successful and records the raw agent output and token usage.
    /// </summary>
    public void Complete(
        string rawAgentOutput,
        TimeSpan duration,
        int inputTokens,
        int outputTokens)
    {
        RawAgentOutput = rawAgentOutput;
        Duration = duration;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = inputTokens + outputTokens;
        Status = RunStatus.Success;
    }

    /// <summary>
    /// Sets the net weekly mood from the parsed agent output.
    /// </summary>
    public void SetMood(MarketSentiment netMood, string moodSummary)
    {
        NetMood = netMood;
        MoodSummary = moodSummary;
    }

    /// <summary>
    /// Adds a consolidated theme to this run.
    /// </summary>
    public void AddTheme(WeeklySummaryTheme theme)
    {
        _themes.Add(theme);
    }

    /// <summary>
    /// Sets the rendered display markdown (generated from structured data).
    /// </summary>
    public void SetDisplayMarkdown(string displayMarkdown)
    {
        RawMarkdownOutput = displayMarkdown;
    }

    /// <summary>
    /// Marks the run as failed.
    /// </summary>
    public void Fail(TimeSpan duration)
    {
        Duration = duration;
        Status = RunStatus.Failed;
    }

    /// <summary>
    /// Downgrades the run status to <see cref="RunStatus.Partial"/> when
    /// parsing succeeded partially but some content was skipped.
    /// </summary>
    public void MarkPartial()
    {
        if (Status == RunStatus.Success)
            Status = RunStatus.Partial;
    }
}
