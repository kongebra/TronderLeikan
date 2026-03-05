namespace TronderLeikan.Application.Games.Commands.UpdateGame;
public record UpdateGameCommand(Guid GameId, string Name, string? Description);
