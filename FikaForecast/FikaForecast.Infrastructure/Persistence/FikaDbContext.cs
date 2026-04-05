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
    public DbSet<CategoryAssessment> CategoryAssessments => Set<CategoryAssessment>();
    public DbSet<WeeklySummaryRun> WeeklySummaryRuns => Set<WeeklySummaryRun>();
    public DbSet<WeeklySummaryTheme> WeeklySummaryThemes => Set<WeeklySummaryTheme>();

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

            entity.HasOne(e => e.Item)
                .WithOne()
                .HasForeignKey<NewsItem>(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.Property(e => e.Mood).HasConversion<string>();

            entity.HasMany(e => e.Assessments)
                .WithOne()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CategoryAssessment>(entity =>
        {
            entity.HasKey(e => e.AssessmentId);
            entity.Property(e => e.Sentiment).HasConversion<string>();
        });

        // Step 2: Weekly Summary
        modelBuilder.Entity<WeeklySummaryRun>(entity =>
        {
            entity.HasKey(e => e.RunId);

            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.NetMood).HasConversion<string>();
            entity.Property(e => e.Duration).HasConversion(
                v => v.TotalMilliseconds,
                v => TimeSpan.FromMilliseconds(v));

            entity.HasMany(e => e.Themes)
                .WithOne()
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklySummaryTheme>(entity =>
        {
            entity.HasKey(e => e.ThemeId);
            entity.Property(e => e.Confidence).HasConversion<string>();
            entity.Property(e => e.Sentiment).HasConversion<string>();
        });
    }
}
