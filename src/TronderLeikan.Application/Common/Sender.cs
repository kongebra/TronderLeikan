using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common;

internal sealed class Sender(IServiceProvider sp) : ISender
{
    // Statiske cacher — unngår gjentatt refleksjon per request
    private static readonly ConcurrentDictionary<Type, MethodInfo> HandlerMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> BehaviorMethodCache = new();

    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) =>
        Dispatch<Result<TResult>>(
            command,
            typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)),
            ct);

    public Task<Result> Send(ICommand command, CancellationToken ct = default) =>
        Dispatch<Result>(
            command,
            typeof(ICommandHandler<>).MakeGenericType(command.GetType()),
            ct);

    public Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default) =>
        Dispatch<Result<TResult>>(
            query,
            typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult)),
            ct);

    private async Task<TResponse> Dispatch<TResponse>(object request, Type handlerType, CancellationToken ct)
    {
        var handler = sp.GetService(handlerType);

        // Hent behaviors for konkret request-type og response-type
        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = sp.GetServices(behaviorType).OfType<object>().ToList();

        // Bygg innerste ledd i pipeline — manglende handler returneres som strukturert feil
        // slik at ObservabilityBehavior m.fl. fanger opp feilen og registrerer trace/metrics
        Func<Task<TResponse>> pipeline;
        if (handler is null)
        {
            var error = Error.Unexpected(
                "Sender.HandlerNotFound",
                $"Ingen handler registrert for '{request.GetType().Name}'.");
            pipeline = () => Task.FromResult((TResponse)(dynamic)error);
        }
        else
        {
            var handleMethod = HandlerMethodCache.GetOrAdd(handlerType, t => t.GetMethod("Handle")!);
            pipeline = () => Invoke<TResponse>(handleMethod, handler, [request, ct]);
        }

        // Pakk inn behaviors — ytterste behavior kjøres først (ObservabilityBehavior → ValidationBehavior → handler/feil)
        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var inner = pipeline;
            var behaviorMethod = BehaviorMethodCache.GetOrAdd(
                behavior.GetType(),
                t => t.GetMethod("Handle")
                    ?? throw new InvalidOperationException(
                        $"Fant ikke 'Handle'-metode på pipeline-behavior '{t.FullName}'. " +
                        "Sjekk at metoden er public og ikke eksplisitt implementert."));
            pipeline = () => Invoke<TResponse>(behaviorMethod, behavior, [request, inner, ct]);
        }

        return await pipeline();
    }

    // Pakker ut TargetInvocationException slik at opprinnelig exception og stack trace bevares
    private static Task<TResponse> Invoke<TResponse>(MethodInfo method, object instance, object[] args)
    {
        try
        {
            return (Task<TResponse>)method.Invoke(instance, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // utilgjengelig — tilfredsstiller kompilatoren
        }
    }
}
