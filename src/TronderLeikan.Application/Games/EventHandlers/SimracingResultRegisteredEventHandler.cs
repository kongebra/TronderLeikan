using Microsoft.Extensions.Logging;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Application.Games.EventHandlers;

// Placeholder — håndterer SimracingResultRegisteredEvent fra RabbitMQ
public sealed class SimracingResultRegisteredEventHandler(ILogger<SimracingResultRegisteredEventHandler> logger)
{
    public Task HandleAsync(SimracingResultRegisteredEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "SimracingResultRegisteredEvent mottatt for spill {GameId}, person {PersonId}",
            @event.GameId, @event.PersonId);
        return Task.CompletedTask;
    }
}
