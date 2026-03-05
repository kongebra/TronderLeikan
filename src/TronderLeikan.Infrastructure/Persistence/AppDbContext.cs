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

        // Skriv til EventStore (append-only audit log)
        foreach (var @event in domainEvents)
        {
            EventStore.Add(new EventStoreEntry
            {
                Id = Guid.NewGuid(),
                StreamId = GetStreamId(@event),
                StreamType = @event.GetType().DeclaringType?.Name ?? @event.GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                Version = 1,
                OccurredAt = clock.UtcNow
            });
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // Finner StreamId basert på hendelsestype — bruker refleksjon for å hente GameId eller annen aggregat-Id
    private static string GetStreamId(IDomainEvent @event)
    {
        var props = @event.GetType().GetProperties();
        var idProp = props.FirstOrDefault(p =>
            p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
            p.PropertyType == typeof(Guid));
        return idProp?.GetValue(@event)?.ToString() ?? Guid.Empty.ToString();
    }
}
