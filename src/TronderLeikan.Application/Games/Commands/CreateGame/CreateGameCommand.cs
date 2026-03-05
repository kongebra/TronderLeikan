using TronderLeikan.Domain.Games;
namespace TronderLeikan.Application.Games.Commands.CreateGame;
public record CreateGameCommand(Guid TournamentId, string Name, GameType GameType);
