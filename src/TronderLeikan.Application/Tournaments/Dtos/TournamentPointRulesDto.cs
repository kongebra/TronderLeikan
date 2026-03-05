namespace TronderLeikan.Application.Tournaments.Dtos;
public record TournamentPointRulesDto(
    int Participation,
    int FirstPlace,
    int SecondPlace,
    int ThirdPlace,
    int OrganizedWithParticipation,
    int OrganizedWithoutParticipation,
    int Spectator);
