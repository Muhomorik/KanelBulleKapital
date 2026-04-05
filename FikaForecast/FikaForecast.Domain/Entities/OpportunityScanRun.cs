using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the Opportunity Scan Agent.
/// Flags the strongest capital rotation destinations worth watching,
/// based on Step 3's substitution chain analysis.
/// </summary>
[DebuggerDisplay("OpScan {Timestamp:yyyy-MM-dd} — {Status} ({TotalTokens} tokens)")]
public class OpportunityScanRun
{
    private readonly List<RotationTarget> _targets = [];

    public Guid RunId { get; private set; }

    /// <summary>FK to the <see cref="SubstitutionChainRun"/> this analysis is based on.</summary>
    public Guid SubstitutionChainRunId { get; private set; }

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

    /// <summary>Rotation targets parsed from the agent output.</summary>
    public IReadOnlyList<RotationTarget> Targets => _targets.AsReadOnly();

    private OpportunityScanRun() // EF Core
    {
        ModelId = null!;
        RawAgentOutput = null!;
        RawMarkdownOutput = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// </summary>
    public static OpportunityScanRun Start(ModelConfig model, Guid substitutionChainRunId)
    {
        return new OpportunityScanRun
        {
            RunId = Guid.NewGuid(),
            SubstitutionChainRunId = substitutionChainRunId,
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
    /// Adds a rotation target entry to this run.
    /// </summary>
    public void AddTarget(RotationTarget target)
    {
        _targets.Add(target);
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
