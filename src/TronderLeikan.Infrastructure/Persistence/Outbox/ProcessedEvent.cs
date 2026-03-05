namespace TronderLeikan.Infrastructure.Persistence.Outbox;

// Idempotens — registrerer allerede behandlede RabbitMQ-meldinger
internal sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
