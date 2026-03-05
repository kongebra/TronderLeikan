namespace TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;

public record UpdateTournamentPointRulesCommand(
    Guid TournamentId,
    int Participation,
    int FirstPlace,
    int SecondPlace,
    int ThirdPlace,
    int OrganizedWithParticipation,
    int OrganizedWithoutParticipation,
    int Spectator);
