using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournaments;

public sealed class GetTournamentsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentsQuery, TournamentSummaryResponse[]>
{
    public async Task<Result<TournamentSummaryResponse[]>> Handle(GetTournamentsQuery query, CancellationToken ct = default)
    {
        var tournaments = await db.Tournaments
            .OrderBy(t => t.Name)
            .Select(t => new TournamentSummaryResponse(t.Id, t.Name, t.Slug))
            .ToArrayAsync(ct);
        return Result<TournamentSummaryResponse[]>.Ok(tournaments);
    }
}
