using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
public record CompleteSimracingGameCommand(Guid GameId) : ICommand;
