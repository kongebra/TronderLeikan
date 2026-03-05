using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Behaviors;
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

        // Pipeline-behaviors — rekkefølge: ObservabilityBehavior ytterst, ValidationBehavior innerst
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // ISender — én avhengighet for alle controllers
        services.AddScoped<ISender, Sender>();

        // Startup-validering — feiler appen hvis handler mangler
        services.AddTransient<IStartupFilter, HandlerRegistrationValidator>();

        // FluentValidation — automatisk registrering av alle validators
        services.AddValidatorsFromAssemblyContaining<IAppDbContext>();

        return services;
    }
}
