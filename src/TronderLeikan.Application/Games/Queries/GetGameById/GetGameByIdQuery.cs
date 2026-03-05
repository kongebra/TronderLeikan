using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetGameById;
public record GetGameByIdQuery(Guid GameId) : IQuery<GameDetailResponse>;
