using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed repository for <see cref="OpportunityScanRun"/> aggregates.
/// </summary>
public class OpportunityScanRunRepository : IOpportunityScanRunRepository
{
    private readonly FikaDbContext _db;

    public OpportunityScanRunRepository(FikaDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(OpportunityScanRun run, CancellationToken cancellationToken = default)
    {
        _db.OpportunityScanRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OpportunityScanRun run, CancellationToken cancellationToken = default)
    {
        _db.OpportunityScanRuns.Update(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<OpportunityScanRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _db.OpportunityScanRuns
            .Include(r => r.Targets)
            .FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityScanRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.OpportunityScanRuns
            .Include(r => r.Targets)
            .ToListAsync(cancellationToken);

        return runs.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.OpportunityScanRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run != null)
        {
            _db.OpportunityScanRuns.Remove(run);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _db.OpportunityScanRuns.RemoveRange(_db.OpportunityScanRuns);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
