using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;
public record CreateTournamentCommand(string Name, string Slug) : ICommand<Guid>;
