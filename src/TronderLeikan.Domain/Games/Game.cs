using TronderLeikan.Domain.Common;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Games;

public sealed class Game : Entity
{
    private readonly List<Guid> _participants = [];
    private readonly List<Guid> _organizers = [];
    private readonly List<Guid> _spectators = [];
    private readonly List<Guid> _firstPlace = [];
    private readonly List<Guid> _secondPlace = [];
    private readonly List<Guid> _thirdPlace = [];

    private Game() { }

    public Guid TournamentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDone { get; private set; }
    public GameType GameType { get; private set; }
    public bool IsOrganizersParticipating { get; private set; }
    public bool HasBanner { get; private set; }

    public IReadOnlyList<Guid> Participants => _participants.AsReadOnly();
    public IReadOnlyList<Guid> Organizers => _organizers.AsReadOnly();
    public IReadOnlyList<Guid> Spectators => _spectators.AsReadOnly();
    public IReadOnlyList<Guid> FirstPlace => _firstPlace.AsReadOnly();
    public IReadOnlyList<Guid> SecondPlace => _secondPlace.AsReadOnly();
    public IReadOnlyList<Guid> ThirdPlace => _thirdPlace.AsReadOnly();

    public static Game Create(string name, Guid tournamentId, GameType gameType = GameType.Standard) =>
        new() { Id = Guid.NewGuid(), Name = name, TournamentId = tournamentId, GameType = gameType };

    // Legg til deltaker — duplikater ignoreres
    public void AddParticipant(Guid personId)
    {
        if (!_participants.Contains(personId))
            _participants.Add(personId);
    }

    // Legg til arrangør med valgfri deltakelse i spillet
    public void AddOrganizer(Guid personId, bool withParticipation)
    {
        if (!_organizers.Contains(personId))
            _organizers.Add(personId);

        if (withParticipation)
            IsOrganizersParticipating = true;
    }

    // Legg til tilskuer — duplikater ignoreres
    public void AddSpectator(Guid personId)
    {
        if (!_spectators.Contains(personId))
            _spectators.Add(personId);
    }

    // Fullfør spillet med plasseringer; støtter ties (flere deler samme plass)
    public void Complete(
        IEnumerable<Guid> firstPlace,
        IEnumerable<Guid> secondPlace,
        IEnumerable<Guid> thirdPlace)
    {
        _firstPlace.Clear();
        _firstPlace.AddRange(firstPlace);
        _secondPlace.Clear();
        _secondPlace.AddRange(secondPlace);
        _thirdPlace.Clear();
        _thirdPlace.AddRange(thirdPlace);

        IsDone = true;
        AddDomainEvent(new GameCompletedEvent(Id));
    }

    public void SetBanner() => HasBanner = true;
    public void RemoveBanner() => HasBanner = false;
    public void UpdateDescription(string? description) => Description = description;
}
