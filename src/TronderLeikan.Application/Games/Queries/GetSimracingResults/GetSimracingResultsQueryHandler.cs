using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetSimracingResults;

public sealed class GetSimracingResultsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetSimracingResultsQuery, SimracingResultResponse[]>
{
    public async Task<Result<SimracingResultResponse[]>> Handle(GetSimracingResultsQuery query, CancellationToken ct = default)
    {
        var results = await db.SimracingResults
            .Where(r => r.GameId == query.GameId)
            .OrderBy(r => r.RaceTimeMs)
            .Select(r => new SimracingResultResponse(r.Id, r.PersonId, r.RaceTimeMs))
            .ToArrayAsync(ct);
        return Result<SimracingResultResponse[]>.Ok(results);
    }
}
