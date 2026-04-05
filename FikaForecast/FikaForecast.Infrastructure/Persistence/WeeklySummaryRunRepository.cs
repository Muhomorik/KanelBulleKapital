using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed repository for <see cref="WeeklySummaryRun"/> aggregates.
/// </summary>
public class WeeklySummaryRunRepository : IWeeklySummaryRunRepository
{
    private readonly FikaDbContext _db;

    public WeeklySummaryRunRepository(FikaDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(WeeklySummaryRun run, CancellationToken cancellationToken = default)
    {
        _db.WeeklySummaryRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeeklySummaryRun?> GetByIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _db.WeeklySummaryRuns
            .Include(r => r.Themes)
            .FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklySummaryRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.WeeklySummaryRuns
            .Include(r => r.Themes)
            .ToListAsync(cancellationToken);

        return runs.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task DeleteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.WeeklySummaryRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run != null)
        {
            _db.WeeklySummaryRuns.Remove(run);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _db.WeeklySummaryRuns.RemoveRange(_db.WeeklySummaryRuns);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
