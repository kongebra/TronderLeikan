namespace TronderLeikan.Application.Tournaments.Responses;
public record TournamentDetailResponse(Guid Id, string Name, string Slug, TournamentPointRulesResponse PointRules);
