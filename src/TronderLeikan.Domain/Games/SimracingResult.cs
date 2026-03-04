using TronderLeikan.Domain.Common;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Games;

// Representerer én persons racetid i et simracing-spill. Tider lagres i millisekunder.
public sealed class SimracingResult : Entity
{
    // EF Core trenger en privat parameterløs konstruktør
    private SimracingResult() { }

    public Guid GameId { get; private set; }
    public Guid PersonId { get; private set; }

    // Racetid i millisekunder
    public long RaceTimeMs { get; private set; }

    // Registrerer en ny simracing-tid og hever domenehendelse
    public static SimracingResult Register(Guid gameId, Guid personId, long raceTimeMs)
    {
        var result = new SimracingResult
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            PersonId = personId,
            RaceTimeMs = raceTimeMs
        };

        result.AddDomainEvent(new SimracingResultRegisteredEvent(gameId, personId));

        return result;
    }
}
