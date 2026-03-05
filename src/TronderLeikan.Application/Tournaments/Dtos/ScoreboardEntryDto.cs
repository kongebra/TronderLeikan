namespace TronderLeikan.Application.Tournaments.Dtos;
public record ScoreboardEntryDto(Guid PersonId, string FirstName, string LastName, int TotalPoints, int Rank);
