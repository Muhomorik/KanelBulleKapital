using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the Opportunity Scan Agent.
/// Flags the strongest capital rotation destinations worth watching,
/// based on Step 3's substitution chain analysis.
/// </summary>
[DebuggerDisplay("OpScan {PeriodIsoWeek} {Timestamp:yyyy-MM-dd} — {Status} ({TotalTokens} tokens)")]
public class OpportunityScanRun
{
    private readonly List<RotationTarget> _targets = [];

    public Guid RunId { get; private set; }

    /// <summary>FK to the <see cref="SubstitutionChainRun"/> this analysis is based on.</summary>
    public Guid SubstitutionChainRunId { get; private set; }

    /// <summary>
    /// Period this opportunity scan covers — denormalized from the parent
    /// <see cref="SubstitutionChainRun"/> (and ultimately the <see cref="WeeklySummaryRun"/>)
    /// at construction so the entity is self-describing.
    /// </summary>
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>ISO 8601 week tag of <see cref="PeriodStart"/>, e.g. <c>"2026-W17"</c>.</summary>
    public string PeriodIsoWeek { get; private set; }

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
        PeriodIsoWeek = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Period fields are denormalized from the parent chain (which inherited them from the weekly summary).
    /// </summary>
    public static OpportunityScanRun Start(
        ModelConfig model,
        Guid substitutionChainRunId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string periodIsoWeek)
    {
        return new OpportunityScanRun
        {
            RunId = Guid.NewGuid(),
            SubstitutionChainRunId = substitutionChainRunId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeriodIsoWeek = periodIsoWeek,
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

    /// <summary>
    /// Reconstitutes a persisted run (e.g. from a sync endpoint) bypassing the Start/Complete lifecycle.
    /// </summary>
    public static OpportunityScanRun Rehydrate(
        Guid runId,
        Guid substitutionChainRunId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string periodIsoWeek,
        DateTimeOffset timestamp,
        string modelId,
        TimeSpan duration,
        int inputTokens,
        int outputTokens,
        int totalTokens,
        RunStatus status,
        string rawAgentOutput,
        string rawMarkdownOutput,
        IEnumerable<RotationTarget> targets)
    {
        var run = new OpportunityScanRun
        {
            RunId = runId,
            SubstitutionChainRunId = substitutionChainRunId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeriodIsoWeek = periodIsoWeek ?? string.Empty,
            Timestamp = timestamp,
            ModelId = modelId,
            Duration = duration,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = totalTokens,
            Status = status,
            RawAgentOutput = rawAgentOutput,
            RawMarkdownOutput = rawMarkdownOutput
        };
        run._targets.AddRange(targets);
        return run;
    }
}
