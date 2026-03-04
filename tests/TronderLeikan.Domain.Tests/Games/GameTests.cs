using FluentAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Tests.Games;

public class GameTests
{
    [Fact]
    public void Create_SetsNavnTournamentIdOgStandardverdier()
    {
        var tournamentId = Guid.NewGuid();

        var game = Game.Create("Kubb", tournamentId);

        game.Id.Should().NotBeEmpty();
        game.Name.Should().Be("Kubb");
        game.TournamentId.Should().Be(tournamentId);
        game.GameType.Should().Be(GameType.Standard);
        game.IsDone.Should().BeFalse();
        game.IsOrganizersParticipating.Should().BeFalse();
        game.HasBanner.Should().BeFalse();
    }

    [Fact]
    public void Create_MedGameType_SetsGameType()
    {
        var game = Game.Create("Simracing", Guid.NewGuid(), GameType.Simracing);

        game.GameType.Should().Be(GameType.Simracing);
    }

    [Fact]
    public void AddParticipant_LeggTilPerson()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddParticipant(personId);

        game.Participants.Should().ContainSingle().Which.Should().Be(personId);
    }

    [Fact]
    public void AddParticipant_DuplikatIgnoreres()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddParticipant(personId);
        game.AddParticipant(personId);

        game.Participants.Should().ContainSingle();
    }

    [Fact]
    public void AddOrganizer_LeggTilArrangør()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddOrganizer(personId, withParticipation: false);

        game.Organizers.Should().ContainSingle().Which.Should().Be(personId);
        game.IsOrganizersParticipating.Should().BeFalse();
    }

    [Fact]
    public void AddOrganizer_MedDeltakelse_SetsIsOrganizersParticipating()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.AddOrganizer(Guid.NewGuid(), withParticipation: true);

        game.IsOrganizersParticipating.Should().BeTrue();
    }

    [Fact]
    public void AddSpectator_LeggTilTilskuer()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddSpectator(personId);

        game.Spectators.Should().ContainSingle().Which.Should().Be(personId);
    }

    [Fact]
    public void Complete_SetsIsDoneOgPlasseringer()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var charlie = Guid.NewGuid();

        game.Complete(
            firstPlace: [alice],
            secondPlace: [bob],
            thirdPlace: [charlie]
        );

        game.IsDone.Should().BeTrue();
        game.FirstPlace.Should().ContainSingle().Which.Should().Be(alice);
        game.SecondPlace.Should().ContainSingle().Which.Should().Be(bob);
        game.ThirdPlace.Should().ContainSingle().Which.Should().Be(charlie);
    }

    [Fact]
    public void Complete_RaiserGameCompletedEvent()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.Complete(firstPlace: [], secondPlace: [], thirdPlace: []);

        game.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GameCompletedEvent>()
            .Which.GameId.Should().Be(game.Id);
    }

    [Fact]
    public void Complete_SupportsTies()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        game.Complete(firstPlace: [alice, bob], secondPlace: [], thirdPlace: []);

        game.FirstPlace.Should().HaveCount(2).And.Contain(alice).And.Contain(bob);
    }

    [Fact]
    public void SetBanner_SetsHasBannerTilTrue()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.SetBanner();

        game.HasBanner.Should().BeTrue();
    }
}
