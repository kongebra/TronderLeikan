using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.CompleteGame;

public sealed class CompleteGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CompleteGameCommand>
{
    public async Task<Result> Handle(CompleteGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return GameErrors.NotFound;
        if (game.IsDone) return GameErrors.AlreadyCompleted;
        game.Complete(command.FirstPlace, command.SecondPlace, command.ThirdPlace);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
