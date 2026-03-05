using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
public record RegisterSimracingResultCommand(Guid GameId, Guid PersonId, long RaceTimeMs) : ICommand<Guid>;
