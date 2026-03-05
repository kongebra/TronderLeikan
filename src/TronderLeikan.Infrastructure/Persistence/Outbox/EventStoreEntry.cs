namespace TronderLeikan.Infrastructure.Persistence.Outbox;

// Append-only audit log per aggregat — støtter tidsreise og replay
internal sealed class EventStoreEntry
{
    public Guid Id { get; set; }

    // StreamId er aggregat-Id (f.eks. GameId)
    public string StreamId { get; set; } = string.Empty;

    // StreamType identifiserer aggregattypen ("Game", "Person")
    public string StreamType { get; set; } = string.Empty;

    // Fullt kvalifisert hendelsesnavn ("GameCompletedEvent")
    public string EventType { get; set; } = string.Empty;

    // Serialisert JSON-payload
    public string Payload { get; set; } = string.Empty;

    // Versjon per stream — brukes til optimistisk samtidighetskontroll
    public int Version { get; set; }

    public DateTime OccurredAt { get; set; }
}
