using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Persists and queries <see cref="WeeklySummaryRun"/> aggregate roots.
/// </summary>
public interface IWeeklySummaryRunRepository
{
    /// <summary>Persists a new run with its themes.</summary>
    Task SaveAsync(WeeklySummaryRun run, CancellationToken cancellationToken = default);

    /// <summary>Returns a run by ID, or <c>null</c> if not found. Includes themes.</summary>
    Task<WeeklySummaryRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Returns all runs ordered by most recent first. Includes themes.</summary>
    Task<IReadOnlyList<WeeklySummaryRun>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes a run by ID.</summary>
    Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Deletes all runs.</summary>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
