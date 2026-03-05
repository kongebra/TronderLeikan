using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
public record GetScoreboardQuery(Guid TournamentId) : IQuery<ScoreboardEntryResponse[]>;
