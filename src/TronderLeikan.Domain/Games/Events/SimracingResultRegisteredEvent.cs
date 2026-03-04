using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Games.Events;

// Domenehendelse som heves når en simracing-tid registreres for et spill
public sealed record SimracingResultRegisteredEvent(Guid GameId, Guid PersonId) : IDomainEvent;
