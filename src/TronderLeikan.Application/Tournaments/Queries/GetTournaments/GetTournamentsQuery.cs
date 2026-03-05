using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournaments;
public record GetTournamentsQuery : IQuery<TournamentSummaryResponse[]>;
