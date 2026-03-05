using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
public record GetTournamentBySlugQuery(string Slug) : IQuery<TournamentDetailResponse>;
