using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.UpdateGame;

public sealed class UpdateGameCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdateGameCommand>
{
    public async Task<Result> Handle(UpdateGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill med Id {command.GameId} finnes ikke.");
        game.UpdateName(command.Name);
        game.UpdateDescription(command.Description);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
