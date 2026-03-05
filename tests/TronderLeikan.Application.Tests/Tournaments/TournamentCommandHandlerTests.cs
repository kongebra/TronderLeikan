using TronderLeikan.Application.Tournaments.Commands.CreateTournament;
using TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Tournaments;

public sealed class TournamentCommandHandlerTests
{
    [Fact]
    public async Task CreateTournament_GyldigInput_LagrerTurnering()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new CreateTournamentCommandHandler(db).Handle(new CreateTournamentCommand("NM 2026", "nm-2026"));
        Assert.True(result.IsSuccess);
        Assert.Single(db.Tournaments.ToList());
    }

    [Fact]
    public async Task UpdateTournamentPointRules_OppdatererRegler()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM 2026", "nm-2026");
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var command = new UpdateTournamentPointRulesCommand(tournament.Id, 5, 4, 3, 2, 2, 4, 1);
        var result = await new UpdateTournamentPointRulesCommandHandler(db).Handle(command);
        Assert.True(result.IsSuccess);
        var updated = await db.Tournaments.FindAsync(tournament.Id);
        Assert.Equal(5, updated!.PointRules.Participation);
    }
}
