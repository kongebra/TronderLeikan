using Microsoft.Extensions.Logging;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Services;

// Placeholder — logger meldingen uten å sende til noen broker
internal sealed class InMemoryMessagePublisher(ILogger<InMemoryMessagePublisher> logger)
    : IMessagePublisher
{
    public Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default)
    {
        logger.LogInformation(
            "InMemoryMessagePublisher: publiserer {Type} til topic '{Topic}'",
            typeof(T).Name, topic ?? "(default)");
        return Task.CompletedTask;
    }
}
