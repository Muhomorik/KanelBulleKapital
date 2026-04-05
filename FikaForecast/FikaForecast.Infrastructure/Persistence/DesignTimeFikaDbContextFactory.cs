using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FikaForecast.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> CLI commands (migrations add, database update, etc.).
/// Uses an in-memory SQLite connection — the actual path is configured at runtime by <c>InfrastructureModule</c>.
/// </summary>
public class DesignTimeFikaDbContextFactory : IDesignTimeDbContextFactory<FikaDbContext>
{
    public FikaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FikaDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new FikaDbContext(options);
    }
}
