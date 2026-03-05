using Microsoft.Extensions.Logging;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Application.Games.EventHandlers;

// Placeholder — håndterer GameCompletedEvent fra RabbitMQ
// Fremtidig: send notifikasjoner, oppdater statistikk, e.l.
public sealed class GameCompletedEventHandler(ILogger<GameCompletedEventHandler> logger)
{
    public Task HandleAsync(GameCompletedEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation("GameCompletedEvent mottatt for spill {GameId}", @event.GameId);
        return Task.CompletedTask;
    }
}
