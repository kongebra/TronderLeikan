using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Commands.CreateGame;

public sealed class CreateGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateGameCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGameCommand command, CancellationToken ct = default)
    {
        var game = Game.Create(command.Name, command.TournamentId, command.GameType);
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
        return game.Id;
    }
}
