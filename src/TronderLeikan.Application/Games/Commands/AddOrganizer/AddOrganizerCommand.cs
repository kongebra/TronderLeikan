using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.AddOrganizer;
public record AddOrganizerCommand(Guid GameId, Guid PersonId, bool WithParticipation) : ICommand;
