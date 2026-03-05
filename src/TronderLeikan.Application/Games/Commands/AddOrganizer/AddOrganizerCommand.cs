namespace TronderLeikan.Application.Games.Commands.AddOrganizer;
public record AddOrganizerCommand(Guid GameId, Guid PersonId, bool WithParticipation);
