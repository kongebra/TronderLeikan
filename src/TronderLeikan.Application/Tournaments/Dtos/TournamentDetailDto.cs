namespace TronderLeikan.Application.Tournaments.Dtos;
public record TournamentDetailDto(Guid Id, string Name, string Slug, TournamentPointRulesDto PointRules);
