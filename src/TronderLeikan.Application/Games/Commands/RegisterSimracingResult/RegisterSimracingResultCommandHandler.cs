using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Commands.RegisterSimracingResult;

public sealed class RegisterSimracingResultCommandHandler(IAppDbContext db)
    : ICommandHandler<RegisterSimracingResultCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterSimracingResultCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return GameErrors.NotFound;
        if (game.IsDone) return GameErrors.AlreadyCompleted;
        var result = SimracingResult.Register(command.GameId, command.PersonId, command.RaceTimeMs);
        db.SimracingResults.Add(result);
        await db.SaveChangesAsync(ct);
        return result.Id;
    }
}
