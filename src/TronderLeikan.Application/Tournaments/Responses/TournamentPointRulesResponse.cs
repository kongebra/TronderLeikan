namespace TronderLeikan.Application.Tournaments.Responses;
public record TournamentPointRulesResponse(
    int Participation,
    int FirstPlace,
    int SecondPlace,
    int ThirdPlace,
    int OrganizedWithParticipation,
    int OrganizedWithoutParticipation,
    int Spectator);
