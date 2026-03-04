namespace TronderLeikan.Application.Common.Interfaces;

// Broker-agnostisk publisering — bytte broker = én linje i DI
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default);
}
