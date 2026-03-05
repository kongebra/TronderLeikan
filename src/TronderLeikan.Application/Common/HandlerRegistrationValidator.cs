using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Common;

// Sjekker at alle commands og queries har registrert handler ved oppstart
// Feiler appen i CI/CD ved manglende registrering — ingen overraskelser i produksjon
internal sealed class HandlerRegistrationValidator(IServiceProvider sp) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ValidateHandlers();
        return next;
    }

    private void ValidateHandlers()
    {
        var assembly = typeof(IAppDbContext).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();
                var args = iface.GetGenericArguments();

                Type? handlerType = def switch
                {
                    var d when d == typeof(ICommand<>) =>
                        typeof(ICommandHandler<,>).MakeGenericType(type, args[0]),
                    var d when d == typeof(IQuery<>) =>
                        typeof(IQueryHandler<,>).MakeGenericType(type, args[0]),
                    _ => null
                };

                if (handlerType is null) continue;

                if (sp.GetService(handlerType) is null)
                    throw new InvalidOperationException(
                        $"Mangler handler-registrering for '{type.Name}'. " +
                        $"Forventet: {handlerType.Name}");
            }

            // Sjekk ICommand uten generics separat
            if (type.GetInterfaces().Any(i => i == typeof(ICommand)))
            {
                var handlerType = typeof(ICommandHandler<>).MakeGenericType(type);
                if (sp.GetService(handlerType) is null)
                    throw new InvalidOperationException(
                        $"Mangler handler-registrering for '{type.Name}'. " +
                        $"Forventet: ICommandHandler<{type.Name}>");
            }
        }
    }
}
