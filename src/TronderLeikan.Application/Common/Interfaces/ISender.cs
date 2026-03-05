using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Mediator-interface — controllers injiserer kun denne
public interface ISender
{
    // Kommando med returverdi (f.eks. opprett-operasjoner som returnerer ny Id)
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);

    // Kommando uten returverdi (f.eks. oppdater, slett)
    Task<Result> Send(ICommand command, CancellationToken ct = default);

    // Query — alltid med returverdi
    Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
