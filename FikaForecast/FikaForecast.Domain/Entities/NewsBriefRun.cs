using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the News Brief Agent.
/// Tracks the model used, token consumption, duration, and parsed output.
/// </summary>
[DebuggerDisplay("{DeploymentName} — {Status} ({TotalTokens} tokens, {Duration.TotalSeconds:F1}s)")]
public class NewsBriefRun
{
    public Guid RunId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string ModelId { get; private set; }
    public string DeploymentName { get; private set; }
    public string PromptName { get; private set; }
    public TimeSpan Duration { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }
    public RunStatus Status { get; private set; }

    /// <summary>Raw JSON response from the agent. Used for evaluation and audit.</summary>
    public string RawAgentOutput { get; private set; }

    /// <summary>Rendered display markdown with emojis, generated from structured data. Used by WebView2 UI.</summary>
    public string RawMarkdownOutput { get; private set; }

    /// <summary>Parsed brief with overall mood and per-category assessments.</summary>
    public NewsItem? Item { get; private set; }

    private NewsBriefRun() // EF Core
    {
        ModelId = null!;
        DeploymentName = null!;
        PromptName = null!;
        RawAgentOutput = null!;
        RawMarkdownOutput = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// </summary>
    public static NewsBriefRun Start(ModelConfig model, AgentPrompt prompt)
    {
        return new NewsBriefRun
        {
            RunId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.Now,
            ModelId = model.ModelId,
            DeploymentName = model.DeploymentName,
            PromptName = prompt.Name,
            Status = RunStatus.Partial,
            RawAgentOutput = string.Empty,
            RawMarkdownOutput = string.Empty
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
    /// Only valid when the current status is <see cref="RunStatus.Success"/>.
    /// </summary>
    public void MarkPartial()
    {
        if (Status == RunStatus.Success)
            Status = RunStatus.Partial;
    }

    /// <summary>
    /// Sets the parsed news item with overall mood and per-category assessments.
    /// </summary>
    public void SetItem(NewsItem item)
    {
        Item = item;
    }
}
