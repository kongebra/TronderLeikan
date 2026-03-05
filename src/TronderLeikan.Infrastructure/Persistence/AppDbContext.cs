using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Common;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDateTimeProvider clock)
    : DbContext(options), IAppDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SimracingResult> SimracingResults => Set<SimracingResult>();
    public DbSet<PersonImage> PersonImages => Set<PersonImage>();
    public DbSet<GameBanner> GameBanners => Set<GameBanner>();
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // Atomisk: state + EventStore + Outbox i én transaksjon
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Samle domenehendelser fra alle entiteter som har dem
        var entities = ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Tøm hendelsesliste før lagring for å unngå dobbel-prosessering
        entities.ForEach(e => e.ClearDomainEvents());

        // Skriv til OutboxMessages (publiseres asynkront av OutboxProcessor)
        foreach (var @event in domainEvents)
        {
            OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = @event.GetType().FullName!,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                CreatedAt = clock.UtcNow
            });
        }

        // Skriv til EventStore (append-only audit log) — versjon økes per stream
        // Beregn stream-IDer én gang for å unngå gjentatt refleksjon
        var eventsWithStreamId = domainEvents
            .Select(e => (Event: e, StreamId: GetStreamId(e)))
            .ToList();

        // Én spørring for å hente maks versjon per stream (unngår N+1)
        var streamIds = eventsWithStreamId.Select(x => x.StreamId).Distinct().ToList();
        var maxVersions = await EventStore
            .Where(e => streamIds.Contains(e.StreamId))
            .GroupBy(e => e.StreamId)
            .Select(g => new { StreamId = g.Key, Max = g.Max(e => e.Version) })
            .ToDictionaryAsync(x => x.StreamId, x => x.Max, cancellationToken);

        var versionPerStream = new Dictionary<string, int>();
        foreach (var (@event, streamId) in eventsWithStreamId)
        {
            if (!versionPerStream.TryGetValue(streamId, out var nextVersion))
                nextVersion = maxVersions.TryGetValue(streamId, out var max) ? max + 1 : 1;

            EventStore.Add(new EventStoreEntry
            {
                Id = Guid.NewGuid(),
                StreamId = streamId,
                StreamType = GetStreamType(@event),
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                Version = nextVersion,
                OccurredAt = clock.UtcNow
            });

            versionPerStream[streamId] = nextVersion + 1;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // Finner StreamId basert på hendelsestype — bruker refleksjon for å hente aggregat-Id
    private static string GetStreamId(IDomainEvent @event)
    {
        var idProp = @event.GetType().GetProperties()
            .FirstOrDefault(p =>
                p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                p.PropertyType == typeof(Guid));

        if (idProp is null)
            throw new InvalidOperationException(
                $"Domeinehendelse '{@event.GetType().Name}' mangler en Guid-egenskap som slutter på 'Id'. " +
                "Legg til en slik egenskap for korrekt stream-gruppering.");

        return idProp.GetValue(@event)!.ToString()!;
    }

    // Henter aggregattypen fra namespace (f.eks. "Games" fra "TronderLeikan.Domain.Games.Events")
    private static string GetStreamType(IDomainEvent @event)
    {
        var parts = @event.GetType().Namespace?.Split('.');
        // Nest siste segment er aggregat-mappen (f.eks. Games, Persons, Tournaments)
        return parts is { Length: >= 2 } ? parts[^2] : "Unknown";
    }
}
