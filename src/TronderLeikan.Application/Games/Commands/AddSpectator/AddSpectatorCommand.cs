using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.AddSpectator;
public record AddSpectatorCommand(Guid GameId, Guid PersonId) : ICommand;
