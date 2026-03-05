namespace TronderLeikan.Application.Common.Interfaces;

// Pipeline-kontrakt — behaviors kjøres i rekkefølge rundt handleren
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
