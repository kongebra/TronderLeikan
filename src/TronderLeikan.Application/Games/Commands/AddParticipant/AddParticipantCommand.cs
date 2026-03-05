using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.AddParticipant;
public record AddParticipantCommand(Guid GameId, Guid PersonId) : ICommand;
