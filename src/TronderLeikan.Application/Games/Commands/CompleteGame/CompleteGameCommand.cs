using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.CompleteGame;
public record CompleteGameCommand(Guid GameId, Guid[] FirstPlace, Guid[] SecondPlace, Guid[] ThirdPlace) : ICommand;
