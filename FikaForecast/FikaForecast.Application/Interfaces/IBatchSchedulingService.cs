using FikaForecast.Application.DTOs;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Builds daily batch schedules and executes batch slots across all configured models.
/// </summary>
public interface IBatchSchedulingService
{
    /// <summary>
    /// Generates the daily time slots for the given interval (e.g. every 4 hours → 6 slots).
    /// Slots whose planned time has already passed are marked as <see cref="PlannedTimeSlot.IsPast"/>.
    /// </summary>
    IReadOnlyList<PlannedTimeSlot> BuildDaySlots(TimeSpan interval);

    /// <summary>
    /// Executes all models sequentially for a single batch slot and returns the aggregated outcome.
    /// </summary>
    Task<BatchSlotResult> ExecuteSlotAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
