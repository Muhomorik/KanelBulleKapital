using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the Substitution Chain Agent.
/// Identifies what sectors, commodities, or themes benefit as capital rotates
/// away from affected areas, based on the weekly summary.
/// </summary>
[DebuggerDisplay("SubChain {Timestamp:yyyy-MM-dd} — {Status} ({TotalTokens} tokens)")]
public class SubstitutionChainRun
{
    private readonly List<RotationChain> _chains = [];

    public Guid RunId { get; private set; }

    /// <summary>FK to the <see cref="WeeklySummaryRun"/> this analysis is based on.</summary>
    public Guid WeeklySummaryRunId { get; private set; }

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

    /// <summary>Rotation chains parsed from the agent output.</summary>
    public IReadOnlyList<RotationChain> Chains => _chains.AsReadOnly();

    private SubstitutionChainRun() // EF Core
    {
        ModelId = null!;
        RawAgentOutput = null!;
        RawMarkdownOutput = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// </summary>
    public static SubstitutionChainRun Start(ModelConfig model, Guid weeklySummaryRunId)
    {
        return new SubstitutionChainRun
        {
            RunId = Guid.NewGuid(),
            WeeklySummaryRunId = weeklySummaryRunId,
            Timestamp = DateTimeOffset.Now,
            ModelId = model.ModelId,
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
    /// Adds a rotation chain entry to this run.
    /// </summary>
    public void AddChain(RotationChain chain)
    {
        _chains.Add(chain);
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
