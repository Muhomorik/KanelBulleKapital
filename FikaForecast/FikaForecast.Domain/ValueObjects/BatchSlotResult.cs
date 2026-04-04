using System.Diagnostics;

namespace FikaForecast.Domain.ValueObjects;

/// <summary>
/// Captures the outcome of executing one batch slot across all configured models.
/// </summary>
/// <param name="SuccessCount">Number of models that completed successfully.</param>
/// <param name="FailureCount">Number of models that failed.</param>
/// <param name="TotalTokens">Total tokens consumed across all successful models.</param>
/// <param name="Duration">Wall-clock time for the entire slot execution.</param>
[DebuggerDisplay("{SuccessCount} ok, {FailureCount} failed, {TotalTokens:N0} tokens")]
public sealed record BatchSlotResult(
    int SuccessCount,
    int FailureCount,
    int TotalTokens,
    TimeSpan Duration)
{
    public bool HasFailures => FailureCount > 0;
}
