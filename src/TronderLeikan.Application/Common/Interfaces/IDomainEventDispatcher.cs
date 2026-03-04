using TronderLeikan.Domain.Common;

namespace TronderLeikan.Application.Common.Interfaces;

// Dispatches domenehendelser etter SaveChanges
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct);
}
