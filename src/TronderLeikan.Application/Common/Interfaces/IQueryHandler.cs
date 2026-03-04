using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Query — alltid med returverdi
public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct = default);
}
