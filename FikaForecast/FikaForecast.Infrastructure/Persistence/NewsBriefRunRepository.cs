using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed repository for <see cref="NewsBriefRun"/> aggregates.
/// </summary>
public class NewsBriefRunRepository : INewsBriefRunRepository
{
    private readonly FikaDbContext _db;

    public NewsBriefRunRepository(FikaDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(NewsBriefRun run, CancellationToken cancellationToken = default)
    {
        _db.NewsBriefRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NewsBriefRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _db.NewsBriefRuns
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
    }

    public async Task<IReadOnlyList<NewsBriefRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.NewsBriefRuns
            .Include(r => r.Items)
            .ToListAsync(cancellationToken);

        return runs.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task<IReadOnlyList<NewsBriefRun>> GetByModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var runs = await _db.NewsBriefRuns
            .Include(r => r.Items)
            .Where(r => r.ModelId == modelId)
            .ToListAsync(cancellationToken);

        return runs.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.NewsBriefRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run != null)
        {
            _db.NewsBriefRuns.Remove(run);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _db.NewsBriefRuns.RemoveRange(_db.NewsBriefRuns);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
