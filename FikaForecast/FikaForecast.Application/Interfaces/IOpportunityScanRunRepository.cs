using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Persists and queries <see cref="OpportunityScanRun"/> aggregate roots.
/// </summary>
public interface IOpportunityScanRunRepository
{
    /// <summary>Persists a new run with its targets.</summary>
    Task SaveAsync(OpportunityScanRun run, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing tracked run.</summary>
    Task UpdateAsync(OpportunityScanRun run, CancellationToken cancellationToken = default);

    /// <summary>Returns a run by ID, or <c>null</c> if not found. Includes targets.</summary>
    Task<OpportunityScanRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Returns all runs ordered by most recent first. Includes targets.</summary>
    Task<IReadOnlyList<OpportunityScanRun>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes a run by ID.</summary>
    Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Deletes all runs.</summary>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
