using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Games.Events;

public sealed record GameCompletedEvent(Guid GameId) : IDomainEvent;
