using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Infrastructure.Services;

// Placeholder — domenehendelser dispatches via SaveChangesAsync i AppDbContext
// Denne klassen er reservert for fremtidig direkte dispatch-behov
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        // Outbox-mønsteret håndteres i AppDbContext.SaveChangesAsync
        return Task.CompletedTask;
    }
}
