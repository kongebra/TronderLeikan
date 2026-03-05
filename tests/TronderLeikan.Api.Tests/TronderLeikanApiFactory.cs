using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using TronderLeikan.Infrastructure;

namespace TronderLeikan.Api.Tests;

public class TronderLeikanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Injiser Testcontainers-connection string — brukes av AddInfrastructure i Program.cs
        builder.UseSetting(
            "ConnectionStrings:tronderleikan",
            _postgres.GetConnectionString());
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Kjør EF Core-migrasjoner mot ekte PostgreSQL via offentlig hjelpemetode
        await Services.MigrateDatabaseAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
