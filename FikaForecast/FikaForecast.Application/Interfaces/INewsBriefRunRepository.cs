using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Persists and queries <see cref="NewsBriefRun"/> aggregate roots.
/// </summary>
/// <remarks>
/// Implemented by <c>NewsBriefRunRepository</c> in the Infrastructure layer using EF Core + SQLite.
/// </remarks>
public interface INewsBriefRunRepository
{
    /// <summary>Persists a new run with its items and mood.</summary>
    Task SaveAsync(NewsBriefRun run, CancellationToken cancellationToken = default);

    /// <summary>Returns a run by ID, or <c>null</c> if not found. Includes items.</summary>
    Task<NewsBriefRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Returns all runs ordered by most recent first. Includes items.</summary>
    Task<IReadOnlyList<NewsBriefRun>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns runs filtered by model, ordered by most recent first.</summary>
    Task<IReadOnlyList<NewsBriefRun>> GetByModelAsync(string modelId, CancellationToken cancellationToken = default);
}
