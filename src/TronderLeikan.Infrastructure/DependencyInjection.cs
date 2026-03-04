using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;

namespace TronderLeikan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Application bruker IAppDbContext — aldri AppDbContext direkte
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
