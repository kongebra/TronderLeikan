using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Dtos;

public record GameDetailDto(
    Guid Id,
    Guid TournamentId,
    string Name,
    string? Description,
    bool IsDone,
    GameType GameType,
    bool HasBanner,
    bool IsOrganizersParticipating,
    IReadOnlyList<Guid> Participants,
    IReadOnlyList<Guid> Organizers,
    IReadOnlyList<Guid> Spectators,
    IReadOnlyList<Guid> FirstPlace,
    IReadOnlyList<Guid> SecondPlace,
    IReadOnlyList<Guid> ThirdPlace);
