using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetSimracingResults;
public record GetSimracingResultsQuery(Guid GameId) : IQuery<SimracingResultResponse[]>;
