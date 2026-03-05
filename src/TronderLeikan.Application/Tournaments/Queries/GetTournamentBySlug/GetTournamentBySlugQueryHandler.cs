using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;

public sealed class GetTournamentBySlugQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentBySlugQuery, TournamentDetailResponse>
{
    public async Task<Result<TournamentDetailResponse>> Handle(GetTournamentBySlugQuery query, CancellationToken ct = default)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(t => t.Slug == query.Slug, ct);
        if (t is null)
            return Result<TournamentDetailResponse>.Fail($"Turnering med slug '{query.Slug}' finnes ikke.");

        var rules = new TournamentPointRulesResponse(
            t.PointRules.Participation, t.PointRules.FirstPlace, t.PointRules.SecondPlace,
            t.PointRules.ThirdPlace, t.PointRules.OrganizedWithParticipation,
            t.PointRules.OrganizedWithoutParticipation, t.PointRules.Spectator);
        return Result<TournamentDetailResponse>.Ok(new TournamentDetailResponse(t.Id, t.Name, t.Slug, rules));
    }
}
