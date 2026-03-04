using FluentAssertions;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class TournamentPointRulesTests
{
    [Fact]
    public void Default_ReturnererRiktigeStandardverdier()
    {
        var rules = TournamentPointRules.Default();

        rules.Participation.Should().Be(3);
        rules.FirstPlace.Should().Be(3);
        rules.SecondPlace.Should().Be(2);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(1);
        rules.OrganizedWithoutParticipation.Should().Be(3);
        rules.Spectator.Should().Be(1);
    }

    [Fact]
    public void Custom_SetsAlleVerdier()
    {
        var rules = TournamentPointRules.Custom(
            participation: 5,
            firstPlace: 5,
            secondPlace: 3,
            thirdPlace: 1,
            organizedWithParticipation: 2,
            organizedWithoutParticipation: 4,
            spectator: 0
        );

        rules.Participation.Should().Be(5);
        rules.FirstPlace.Should().Be(5);
        rules.SecondPlace.Should().Be(3);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(2);
        rules.OrganizedWithoutParticipation.Should().Be(4);
        rules.Spectator.Should().Be(0);
    }
}
