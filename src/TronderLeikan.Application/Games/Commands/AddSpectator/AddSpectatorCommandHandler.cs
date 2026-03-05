using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddSpectator;

public sealed class AddSpectatorCommandHandler(IAppDbContext db)
    : ICommandHandler<AddSpectatorCommand>
{
    public async Task<Result> Handle(AddSpectatorCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        game.AddSpectator(command.PersonId);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
