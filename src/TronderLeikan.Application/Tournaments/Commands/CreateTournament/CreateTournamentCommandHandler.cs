using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;

public sealed class CreateTournamentCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateTournamentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTournamentCommand command, CancellationToken ct = default)
    {
        var tournament = Tournament.Create(command.Name, command.Slug);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(tournament.Id);
    }
}
