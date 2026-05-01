using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the Substitution Chain Agent.
/// Identifies what sectors, commodities, or themes benefit as capital rotates
/// away from affected areas, based on the weekly summary.
/// </summary>
[DebuggerDisplay("SubChain {PeriodIsoWeek} {Timestamp:yyyy-MM-dd} — {Status} ({TotalTokens} tokens)")]
public class SubstitutionChainRun
{
    private readonly List<RotationChain> _chains = [];

    public Guid RunId { get; private set; }

    /// <summary>FK to the <see cref="WeeklySummaryRun"/> this analysis is based on.</summary>
    public Guid WeeklySummaryRunId { get; private set; }

    /// <summary>
    /// Period this chain analysis covers — denormalized from the parent
    /// <see cref="WeeklySummaryRun"/> at construction so the entity is self-describing.
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

    /// <summary>Rotation chains parsed from the agent output.</summary>
    public IReadOnlyList<RotationChain> Chains => _chains.AsReadOnly();

    private SubstitutionChainRun() // EF Core
    {
        ModelId = null!;
        RawAgentOutput = null!;
        RawMarkdownOutput = null!;
        PeriodIsoWeek = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// Period fields are denormalized from the parent <see cref="WeeklySummaryRun"/>.
    /// </summary>
    public static SubstitutionChainRun Start(
        ModelConfig model,
        Guid weeklySummaryRunId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string periodIsoWeek)
    {
        return new SubstitutionChainRun
        {
            RunId = Guid.NewGuid(),
            WeeklySummaryRunId = weeklySummaryRunId,
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

    /// <summary>
    /// Reconstitutes a persisted run (e.g. from a sync endpoint) bypassing the Start/Complete lifecycle.
    /// </summary>
    public static SubstitutionChainRun Rehydrate(
        Guid runId,
        Guid weeklySummaryRunId,
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
        IEnumerable<RotationChain> chains)
    {
        var run = new SubstitutionChainRun
        {
            RunId = runId,
            WeeklySummaryRunId = weeklySummaryRunId,
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
        run._chains.AddRange(chains);
        return run;
    }
}
