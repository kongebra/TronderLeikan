using FluentAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Tests.Games;

public class SimracingResultTests
{
    [Fact]
    public void Register_SetsAlleVerdier()
    {
        var gameId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var result = SimracingResult.Register(gameId, personId, raceTimeMs: 92_543);

        result.Id.Should().NotBeEmpty();
        result.GameId.Should().Be(gameId);
        result.PersonId.Should().Be(personId);
        result.RaceTimeMs.Should().Be(92_543);
    }

    [Fact]
    public void Register_RaiserSimracingResultRegisteredEvent()
    {
        var gameId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var result = SimracingResult.Register(gameId, personId, raceTimeMs: 88_000);

        result.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SimracingResultRegisteredEvent>()
            .Which.GameId.Should().Be(gameId);
    }
}
