using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;

public sealed class UpdateTournamentPointRulesCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdateTournamentPointRulesCommand>
{
    public async Task<Result> Handle(UpdateTournamentPointRulesCommand command, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([command.TournamentId], ct);
        if (tournament is null)
            return Result.Fail($"Turnering med Id {command.TournamentId} finnes ikke.");

        tournament.UpdatePointRules(TournamentPointRules.Custom(
            command.Participation, command.FirstPlace, command.SecondPlace, command.ThirdPlace,
            command.OrganizedWithParticipation, command.OrganizedWithoutParticipation, command.Spectator));

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
