using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetGameById;

public sealed class GetGameByIdQueryHandler(IAppDbContext db)
    : IQueryHandler<GetGameByIdQuery, GameDetailResponse>
{
    public async Task<Result<GameDetailResponse>> Handle(GetGameByIdQuery query, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([query.GameId], ct);
        if (game is null) return Result<GameDetailResponse>.Fail($"Spill {query.GameId} finnes ikke.");
        return Result<GameDetailResponse>.Ok(new GameDetailResponse(
            game.Id, game.TournamentId, game.Name, game.Description,
            game.IsDone, game.GameType, game.HasBanner, game.IsOrganizersParticipating,
            game.Participants, game.Organizers, game.Spectators,
            game.FirstPlace, game.SecondPlace, game.ThirdPlace));
    }
}
