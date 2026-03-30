using FikaForecast.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// EF Core context for FikaForecast. Uses SQLite for local desktop persistence.
/// </summary>
public class FikaDbContext : DbContext
{
    public DbSet<NewsBriefRun> NewsBriefRuns => Set<NewsBriefRun>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();

    public FikaDbContext(DbContextOptions<FikaDbContext> options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NewsBriefRun>(entity =>
        {
            entity.HasKey(e => e.RunId);

            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Duration).HasConversion(
                v => v.TotalMilliseconds,
                v => TimeSpan.FromMilliseconds(v));

            entity.OwnsOne(e => e.Mood, mood =>
            {
                mood.Property(m => m.DominantSentiment).HasConversion<string>();
            });

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.Sentiment).HasConversion<string>();
        });
    }
}
