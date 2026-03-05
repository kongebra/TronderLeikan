using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.UpdateGame;

public sealed class UpdateGameCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdateGameCommand>
{
    public async Task<Result> Handle(UpdateGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return GameErrors.NotFound;
        game.UpdateName(command.Name);
        game.UpdateDescription(command.Description);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
