using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Persistence;

// Brukes kun av EF Core design-time verktøy (dotnet ef migrations)
// Ikke registrert i DI — kun for CLI
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=tronderleikan_dev;Username=postgres;Password=postgres")
            .Options;
        // Stub for design-time — IDateTimeProvider trengs ikke ved migrasjonskjøring
        return new AppDbContext(options, new DesignTimeDateTimeProvider());
    }

    // Brukes kun under dotnet ef migrations — aldri i produksjon
    private sealed class DesignTimeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
