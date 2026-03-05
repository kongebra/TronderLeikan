using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler(IAppDbContext db)
    : ICommandHandler<AddParticipantCommand>
{
    public async Task<Result> Handle(AddParticipantCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return GameErrors.NotFound;
        game.AddParticipant(command.PersonId);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
