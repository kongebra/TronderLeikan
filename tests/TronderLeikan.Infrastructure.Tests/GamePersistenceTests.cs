using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Tournaments;
using TronderLeikan.Infrastructure.Persistence;

namespace TronderLeikan.Infrastructure.Tests;

// Disse testene krever Docker kjørende på maskinen
public sealed class GamePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Migrer skjemaet én gang for alle testene i klassen
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Game_MedDeltakere_KanLagresOgHentes()
    {
        await using var context = CreateContext();

        // Opprett et spill med deltakere
        var tournamentId = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var game = Game.Create("Kubb", tournamentId);
        game.AddParticipant(alice);
        game.AddParticipant(bob);

        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Hent spillet fra databasen
        var lagretGame = await context.Games.FindAsync(game.Id);

        lagretGame.Should().NotBeNull();
        lagretGame!.Participants.Should().HaveCount(2)
            .And.Contain(alice)
            .And.Contain(bob);
    }

    [Fact]
    public async Task Game_Complete_LagrerPlasseringer()
    {
        await using var context = CreateContext();

        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var charlie = Guid.NewGuid();

        var game = Game.Create("Dart", Guid.NewGuid());
        game.Complete(
            firstPlace: [alice],
            secondPlace: [bob],
            thirdPlace: [charlie]
        );

        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Last på nytt fra DB
        context.ChangeTracker.Clear();
        var lagretGame = await context.Games.FindAsync(game.Id);

        lagretGame!.IsDone.Should().BeTrue();
        lagretGame.FirstPlace.Should().ContainSingle().Which.Should().Be(alice);
        lagretGame.SecondPlace.Should().ContainSingle().Which.Should().Be(bob);
        lagretGame.ThirdPlace.Should().ContainSingle().Which.Should().Be(charlie);
    }

    [Fact]
    public async Task Tournament_MedPointRules_KanLagresOgHentes()
    {
        await using var context = CreateContext();

        var tournament = Tournament.Create("Høst-leikan", "host-leikan");
        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var lagretTournament = await context.Tournaments.FindAsync(tournament.Id);

        lagretTournament!.PointRules.Participation.Should().Be(3);
        lagretTournament.PointRules.FirstPlace.Should().Be(3);
    }
}
