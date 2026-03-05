using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;
using TronderLeikan.Infrastructure.Services;

namespace TronderLeikan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Registrer IDateTimeProvider først — brukes av AppDbContext
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IImageProcessor, ImageSharpImageProcessor>();
        services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Application bruker IAppDbContext — aldri AppDbContext direkte
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
