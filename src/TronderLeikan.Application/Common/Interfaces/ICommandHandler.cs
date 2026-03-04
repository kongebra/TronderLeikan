using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Kommando med returverdi (f.eks. ny Id)
public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct = default);
}

// Kommando uten returverdi (f.eks. slett, oppdater)
public interface ICommandHandler<TCommand>
{
    Task<Result> Handle(TCommand command, CancellationToken ct = default);
}
