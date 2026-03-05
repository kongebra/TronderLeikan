namespace TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
public record RegisterSimracingResultCommand(Guid GameId, Guid PersonId, long RaceTimeMs);
