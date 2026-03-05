using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddOrganizer;

public sealed class AddOrganizerCommandHandler(IAppDbContext db)
    : ICommandHandler<AddOrganizerCommand>
{
    public async Task<Result> Handle(AddOrganizerCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        game.AddOrganizer(command.PersonId, command.WithParticipation);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
