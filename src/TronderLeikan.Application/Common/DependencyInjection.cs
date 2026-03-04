using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Automatisk registrering av alle handlers via Scrutor
        services.Scan(scan => scan
            .FromAssemblyOf<IAppDbContext>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime());

        // FluentValidation — automatisk registrering av alle validators i assembly
        services.AddValidatorsFromAssemblyContaining<IAppDbContext>();

        return services;
    }
}
