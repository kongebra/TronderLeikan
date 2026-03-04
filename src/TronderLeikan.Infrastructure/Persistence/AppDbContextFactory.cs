using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
        return new AppDbContext(options);
    }
}
