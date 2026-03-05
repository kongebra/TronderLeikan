using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.CompleteSimracingGame;

public sealed class CompleteSimracingGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CompleteSimracingGameCommand>
{
    public async Task<Result> Handle(CompleteSimracingGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        if (game.IsDone) return Result.Fail("Spillet er allerede fullført.");

        var results = await db.SimracingResults
            .Where(r => r.GameId == command.GameId)
            .OrderBy(r => r.RaceTimeMs)
            .ToListAsync(ct);

        if (results.Count == 0)
            return Result.Fail("Ingen racetider registrert for dette spillet.");

        // Grupper like tider — deler plassering (ties)
        var groups = results.GroupBy(r => r.RaceTimeMs).OrderBy(g => g.Key).ToList();

        var firstPlace = groups.Count > 0 ? groups[0].Select(r => r.PersonId).ToArray() : [];
        var secondPlace = groups.Count > 1 ? groups[1].Select(r => r.PersonId).ToArray() : [];
        var thirdPlace = groups.Count > 2 ? groups[2].Select(r => r.PersonId).ToArray() : [];

        game.Complete(firstPlace, secondPlace, thirdPlace);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
