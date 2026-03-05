using System.Collections.Concurrent;
using System.Reflection;
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

        // Graceful fallback — returnerer strukturert feil i stedet for ukontrollert exception
        if (handler is null)
        {
            var error = Error.Unexpected(
                "Sender.HandlerNotFound",
                $"Ingen handler registrert for '{request.GetType().Name}'.");
            return (TResponse)(dynamic)error;
        }

        // Hent behaviors for konkret request-type og response-type
        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = sp.GetServices(behaviorType).ToList();

        // Bygg pipeline — ytterste behavior kjøres først (ObservabilityBehavior → ValidationBehavior → handler)
        var handleMethod = HandlerMethodCache.GetOrAdd(handlerType, t => t.GetMethod("Handle")!);
        Func<Task<TResponse>> pipeline = () =>
            (Task<TResponse>)handleMethod.Invoke(handler, [request, ct])!;

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var inner = pipeline;
            var behaviorMethod = BehaviorMethodCache.GetOrAdd(behavior.GetType(), t => t.GetMethod("Handle")!);
            pipeline = () => (Task<TResponse>)behaviorMethod.Invoke(behavior, [request, inner, ct])!;
        }

        return await pipeline();
    }
}
