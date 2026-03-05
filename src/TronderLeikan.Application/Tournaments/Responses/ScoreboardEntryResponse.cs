namespace TronderLeikan.Application.Tournaments.Responses;
public record ScoreboardEntryResponse(Guid PersonId, string FirstName, string LastName, int TotalPoints, int Rank);
