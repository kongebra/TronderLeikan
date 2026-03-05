namespace TronderLeikan.Infrastructure.Persistence.Outbox;

// Domenehendelse som venter på publisering til RabbitMQ
internal sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
