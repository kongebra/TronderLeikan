using TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
using TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
using TronderLeikan.Application.Games.Queries.GetSimracingResults;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Games;

public sealed class SimracingHandlerTests
{
    [Fact]
    public async Task RegisterSimracingResult_LagrerResultat()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var person = Person.Create("Ola", "Nordmann");
        db.Games.Add(game);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new RegisterSimracingResultCommandHandler(db).Handle(new RegisterSimracingResultCommand(game.Id, person.Id, 93500L));
        Assert.True(result.IsSuccess);
        Assert.Single(db.SimracingResults.ToList());
    }

    [Fact]
    public async Task CompleteSimracingGame_BeregnerPlasseringerFraRacetider()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var personA = Person.Create("A", "A");
        var personB = Person.Create("B", "B");
        var personC = Person.Create("C", "C");
        db.Games.Add(game);
        db.Persons.AddRange(personA, personB, personC);
        db.SimracingResults.AddRange(
            SimracingResult.Register(game.Id, personA.Id, 90000L),
            SimracingResult.Register(game.Id, personB.Id, 95000L),
            SimracingResult.Register(game.Id, personC.Id, 92000L));
        await db.SaveChangesAsync();
        var result = await new CompleteSimracingGameCommandHandler(db).Handle(new CompleteSimracingGameCommand(game.Id));
        Assert.True(result.IsSuccess);
        var completed = await db.Games.FindAsync(game.Id);
        Assert.True(completed!.IsDone);
        Assert.Contains(personA.Id, completed.FirstPlace);
        Assert.Contains(personC.Id, completed.SecondPlace);
        Assert.Contains(personB.Id, completed.ThirdPlace);
    }

    [Fact]
    public async Task CompleteSimracingGame_Ties_DelerPlassering()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var personA = Person.Create("A", "A");
        var personB = Person.Create("B", "B");
        db.Games.Add(game);
        db.Persons.AddRange(personA, personB);
        db.SimracingResults.AddRange(
            SimracingResult.Register(game.Id, personA.Id, 90000L),
            SimracingResult.Register(game.Id, personB.Id, 90000L));
        await db.SaveChangesAsync();
        var result = await new CompleteSimracingGameCommandHandler(db).Handle(new CompleteSimracingGameCommand(game.Id));
        Assert.True(result.IsSuccess);
        var completed = await db.Games.FindAsync(game.Id);
        Assert.Contains(personA.Id, completed!.FirstPlace);
        Assert.Contains(personB.Id, completed.FirstPlace);
        Assert.Empty(completed.SecondPlace);
    }
}
