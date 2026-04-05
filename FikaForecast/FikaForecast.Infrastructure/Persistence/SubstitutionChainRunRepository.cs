using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed repository for <see cref="SubstitutionChainRun"/> aggregates.
/// </summary>
public class SubstitutionChainRunRepository : ISubstitutionChainRunRepository
{
    private readonly FikaDbContext _db;

    public SubstitutionChainRunRepository(FikaDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(SubstitutionChainRun run, CancellationToken cancellationToken = default)
    {
        _db.SubstitutionChainRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SubstitutionChainRun run, CancellationToken cancellationToken = default)
    {
        _db.SubstitutionChainRuns.Update(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubstitutionChainRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _db.SubstitutionChainRuns
            .Include(r => r.Chains)
            .FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
    }

    public async Task<IReadOnlyList<SubstitutionChainRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.SubstitutionChainRuns
            .Include(r => r.Chains)
            .ToListAsync(cancellationToken);

        return runs.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.SubstitutionChainRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run != null)
        {
            _db.SubstitutionChainRuns.Remove(run);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _db.SubstitutionChainRuns.RemoveRange(_db.SubstitutionChainRuns);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
