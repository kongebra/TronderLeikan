using FluentAssertions;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class TournamentTests
{
    [Fact]
    public void Create_SetsNavnSlugOgId()
    {
        var tournament = Tournament.Create("Høst-leikan 2026", "host-leikan-2026");

        tournament.Id.Should().NotBeEmpty();
        tournament.Name.Should().Be("Høst-leikan 2026");
        tournament.Slug.Should().Be("host-leikan-2026");
    }

    [Fact]
    public void Create_HarStandardpoengregler()
    {
        var tournament = Tournament.Create("Test", "test");

        tournament.PointRules.Participation.Should().Be(3);
        tournament.PointRules.FirstPlace.Should().Be(3);
    }

    [Fact]
    public void UpdatePointRules_EndrerRegler()
    {
        var tournament = Tournament.Create("Test", "test");
        var nyeRegler = TournamentPointRules.Custom(5, 5, 3, 1, 2, 4, 0);

        tournament.UpdatePointRules(nyeRegler);

        tournament.PointRules.Participation.Should().Be(5);
    }
}
