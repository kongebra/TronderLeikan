using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Tournaments;

public sealed class Tournament : Entity
{
    private Tournament() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public TournamentPointRules PointRules { get; private set; } = TournamentPointRules.Default();

    public static Tournament Create(string name, string slug) =>
        new() { Id = Guid.NewGuid(), Name = name, Slug = slug };

    public void UpdatePointRules(TournamentPointRules rules) => PointRules = rules;
}
