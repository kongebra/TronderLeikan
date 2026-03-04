namespace TronderLeikan.Domain.Tournaments;

public sealed class TournamentPointRules
{
    private TournamentPointRules() { }

    public int Participation { get; private set; } = 3;
    public int FirstPlace { get; private set; } = 3;
    public int SecondPlace { get; private set; } = 2;
    public int ThirdPlace { get; private set; } = 1;
    public int OrganizedWithParticipation { get; private set; } = 1;
    public int OrganizedWithoutParticipation { get; private set; } = 3;
    public int Spectator { get; private set; } = 1;

    public static TournamentPointRules Default() => new();

    public static TournamentPointRules Custom(
        int participation,
        int firstPlace,
        int secondPlace,
        int thirdPlace,
        int organizedWithParticipation,
        int organizedWithoutParticipation,
        int spectator) =>
        new()
        {
            Participation = participation,
            FirstPlace = firstPlace,
            SecondPlace = secondPlace,
            ThirdPlace = thirdPlace,
            OrganizedWithParticipation = organizedWithParticipation,
            OrganizedWithoutParticipation = organizedWithoutParticipation,
            Spectator = spectator
        };
}
