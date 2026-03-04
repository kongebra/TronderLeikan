# Application Layer Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Implementer Application-laget (CQRS-handlere, Result-pattern, interfaces, validerere) og tilhørende Infrastructure-utvidelser (EventStore, Outbox, OutboxProcessor, implementasjoner av IDateTimeProvider, IImageProcessor, IDomainEventDispatcher).

**Architecture:** Egne `ICommandHandler`/`IQueryHandler`-interfaces uten MediatR. FluentValidation for input. Result-pattern for feilhåndtering. AppDbContext-override som atomisk skriver state + EventStore + Outbox i én transaksjon. OutboxProcessor som IHostedService publiserer til IMessagePublisher.

**Tech Stack:** .NET 10, EF Core 10, FluentValidation, Scrutor, SixLabors.ImageSharp, Testcontainers.PostgreSql (eksisterende), xUnit, EF Core InMemory (for Application-enhetstester)

**Viktige konvensjoner:**
- Kode på engelsk, kommentarer på norsk (med æøå)
- `file`-scoped namespaces, primary constructors, implicit usings
- Alltid `Result<T>` fra handlers — aldri kast exception for forventede feil
- Aldri endre domenelogikk — les `Game.cs`, `Person.cs`, `Tournament.cs` for hva som fins

---

## Task 1: NuGet-pakker + Result-pattern + handler-interfaces

**Files:**
- Modify: `src/TronderLeikan.Application/TronderLeikan.Application.csproj`
- Create: `src/TronderLeikan.Application/Common/Results/Result.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/ICommandHandler.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/IQueryHandler.cs`

**Step 1: Legg til NuGet-pakker i Application.csproj**

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" Version="12.0.0" />
  <PackageReference Include="Scrutor" Version="5.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.3" />
</ItemGroup>
```

Kjør: `dotnet restore src/TronderLeikan.Application/TronderLeikan.Application.csproj`
Forventet: Ingen feil.

**Step 2: Skriv Result.cs**

```csharp
namespace TronderLeikan.Application.Common.Results;

// Ikke-generisk Result for kommandoer uten returverdi
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool success, string? error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

// Generisk Result for kommandoer og queries med returverdi
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) { Value = value; }
    private Result(string error) : base(false, error) { }

    public static Result<T> Ok(T value) => new(value);
    public new static Result<T> Fail(string error) => new(error);
}
```

**Step 3: Skriv ICommandHandler.cs**

```csharp
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Kommando med returverdi (f.eks. ny Id)
public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct = default);
}

// Kommando uten returverdi (f.eks. slett, oppdater)
public interface ICommandHandler<TCommand>
{
    Task<Result> Handle(TCommand command, CancellationToken ct = default);
}
```

**Step 4: Skriv IQueryHandler.cs**

```csharp
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Query — alltid med returverdi
public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct = default);
}
```

**Step 5: Bygg**

```bash
dotnet build src/TronderLeikan.Application/TronderLeikan.Application.csproj
```
Forventet: Build succeeded, 0 errors.

**Step 6: Commit**

```bash
git add src/TronderLeikan.Application/
git commit -m "feat: legg til Result-pattern og handler-interfaces i Application"
```

---

## Task 2: Infrastruktur-interfaces i Application

**Files:**
- Create: `src/TronderLeikan.Application/Common/Interfaces/IMessagePublisher.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/IDomainEventDispatcher.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/IImageProcessor.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/IDateTimeProvider.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/ICurrentUser.cs`

**Step 1: IMessagePublisher.cs**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Broker-agnostisk publisering — bytte broker = én linje i DI
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default);
}
```

**Step 2: IDomainEventDispatcher.cs**

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Application.Common.Interfaces;

// Dispatches domenehendelser etter SaveChanges
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct);
}
```

**Step 3: IImageProcessor.cs**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Abstraksjon mot bildeprosessering — Application kjenner ikke til ImageSharp
public interface IImageProcessor
{
    Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct);
    Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct);
}
```

**Step 4: IDateTimeProvider.cs**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Testbar klokke — unngår spredte DateTime.UtcNow-kall
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
```

**Step 5: ICurrentUser.cs**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Stub for fremtidig autentisering (Zitadel/Entra ID)
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
```

**Step 6: Bygg og commit**

```bash
dotnet build src/TronderLeikan.Application/TronderLeikan.Application.csproj
git add src/TronderLeikan.Application/Common/Interfaces/
git commit -m "feat: legg til infrastruktur-interfaces i Application"
```

---

## Task 3: DependencyInjection.cs for Application (Scrutor)

**Files:**
- Create: `src/TronderLeikan.Application/Common/DependencyInjection.cs`

**Step 1: Skriv DependencyInjection.cs**

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Automatisk registrering av alle handlers via Scrutor
        services.Scan(scan => scan
            .FromAssemblyOf<IAppDbContext>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime());

        // FluentValidation — automatisk registrering av alle validators i assembly
        services.AddValidatorsFromAssemblyContaining<IAppDbContext>();

        return services;
    }
}
```

**Step 2: Registrer AddApplication i API**

Åpne `src/TronderLeikan.API/Program.cs`. Legg til etter `builder.AddServiceDefaults()`:
```csharp
// Registrer Application-laget (handlers + validators)
builder.Services.AddApplication();
```

Legg til using øverst:
```csharp
using TronderLeikan.Application.Common;
```

**Step 3: Legg til referanse til Application i API.csproj** (om den mangler):
```xml
<ProjectReference Include="../TronderLeikan.Application/TronderLeikan.Application.csproj" />
```

**Step 4: Bygg**

```bash
dotnet build src/TronderLeikan.API/TronderLeikan.API.csproj
```
Forventet: Build succeeded.

**Step 5: Commit**

```bash
git add src/TronderLeikan.Application/ src/TronderLeikan.API/
git commit -m "feat: legg til DependencyInjection med Scrutor for Application"
```

---

## Task 4: Test-prosjekt for Application (TronderLeikan.Application.Tests)

Opprett nytt xUnit-testprosjekt isolert fra Infrastructure. Bruker EF Core InMemory for å unngå avhengighet til Testcontainers.

**Files:**
- Create: `tests/TronderLeikan.Application.Tests/TronderLeikan.Application.Tests.csproj`
- Create: `tests/TronderLeikan.Application.Tests/TestAppDbContext.cs`
- Modify: `TronderLeikan.slnx` (legg til prosjektet)

**Step 1: Opprett test.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.3" />
    <PackageReference Include="FluentValidation" Version="12.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/TronderLeikan.Application/TronderLeikan.Application.csproj" />
    <ProjectReference Include="../../src/TronderLeikan.Domain/TronderLeikan.Domain.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Legg til i solution**

```bash
dotnet sln TronderLeikan.slnx add tests/TronderLeikan.Application.Tests/TronderLeikan.Application.Tests.csproj
```

**Step 3: Opprett TestAppDbContext.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests;

// Enkel InMemory-kontekst for Application-tester — ingen Infrastructure-avhengighet
internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SimracingResult> SimracingResults => Set<SimracingResult>();
    public DbSet<PersonImage> PersonImages => Set<PersonImage>();
    public DbSet<GameBanner> GameBanners => Set<GameBanner>();

    // Hjelpemetode for enkel oppsett i tester
    internal static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestAppDbContext(options);
    }
}
```

**Step 4: Bygg og kjør (ingen tester ennå)**

```bash
dotnet build tests/TronderLeikan.Application.Tests/
```
Forventet: Build succeeded.

**Step 5: Commit**

```bash
git add tests/TronderLeikan.Application.Tests/ TronderLeikan.slnx
git commit -m "test: legg til Application.Tests-prosjekt med InMemory TestAppDbContext"
```

---

## Task 5: Infrastructure — EventStore + OutboxMessage + ProcessedEvent

Tre nye Infrastructure-entiteter + EF Core-konfigurasjoner for outbox-mønsteret.

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/Outbox/EventStoreEntry.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Outbox/OutboxMessage.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Outbox/ProcessedEvent.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/EventStoreEntryConfiguration.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/ProcessedEventConfiguration.cs`
- Modify: `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`

**Step 1: EventStoreEntry.cs**

```csharp
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
```

**Step 2: OutboxMessage.cs**

```csharp
namespace TronderLeikan.Infrastructure.Persistence.Outbox;

// Domenehendelse som venter på publisering til RabbitMQ
internal sealed class OutboxMessage
{
    public Guid Id { get; set; }

    // Fullt kvalifisert hendelsesnavn
    public string Type { get; set; } = string.Empty;

    // Serialisert JSON-payload
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Null = ikke behandlet, satt av OutboxProcessor
    public DateTime? ProcessedAt { get; set; }

    // Satt hvis publisering feilet
    public string? Error { get; set; }
}
```

**Step 3: ProcessedEvent.cs**

```csharp
namespace TronderLeikan.Infrastructure.Persistence.Outbox;

// Idempotens — registrerer allerede behandlede RabbitMQ-meldinger
internal sealed class ProcessedEvent
{
    // RabbitMQ message ID
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

**Step 4: EF Core-konfigurasjoner**

`EventStoreEntryConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class EventStoreEntryConfiguration : IEntityTypeConfiguration<EventStoreEntry>
{
    public void Configure(EntityTypeBuilder<EventStoreEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StreamId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.StreamType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        // Indeks for tidsreise-queries: SELECT * WHERE StreamId = @id ORDER BY Version
        builder.HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
        builder.ToTable("EventStore");
    }
}
```

`OutboxMessageConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Type).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.ProcessedAt);
        builder.Property(o => o.Error).HasMaxLength(2000);

        // Indeks for OutboxProcessor — henter kun ubehandlede meldinger
        builder.HasIndex(o => o.ProcessedAt);
        builder.ToTable("OutboxMessages");
    }
}
```

`ProcessedEventConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.HasKey(p => p.EventId);
        builder.Property(p => p.ProcessedAt).IsRequired();
        builder.ToTable("ProcessedEvents");
    }
}
```

**Step 5: Legg til DbSet-er i AppDbContext**

Åpne `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`. Legg til øverst i klassen:
```csharp
using TronderLeikan.Infrastructure.Persistence.Outbox;
```

Legg til DbSet-er i AppDbContext-klassen (etter de eksisterende):
```csharp
public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
```

**Step 6: Bygg**

```bash
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```
Forventet: Build succeeded.

**Step 7: Commit**

```bash
git add src/TronderLeikan.Infrastructure/
git commit -m "feat: legg til EventStore, OutboxMessage og ProcessedEvent entiteter og konfigurasjoner"
```

---

## Task 6: Infrastructure-implementasjoner (DateTimeProvider + ImageProcessor)

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Services/SystemDateTimeProvider.cs`
- Create: `src/TronderLeikan.Infrastructure/Services/ImageSharpImageProcessor.cs`

**Step 1: Legg til ImageSharp i Infrastructure.csproj**

```xml
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />
```

**Step 2: SystemDateTimeProvider.cs**

```csharp
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Services;

// Produksjonsimplementasjon av IDateTimeProvider
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
```

**Step 3: ImageSharpImageProcessor.cs**

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Services;

// Konverterer og resizeer bilder med ImageSharp — Application vet ikke om ImageSharp
internal sealed class ImageSharpImageProcessor : IImageProcessor
{
    // Profilbilder: 256×256 px, WebP
    public async Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(ctx => ctx.Resize(256, 256));
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder(), ct);
        return ms.ToArray();
    }

    // Spillbannere: 1200×400 px, WebP
    public async Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(ctx => ctx.Resize(1200, 400));
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder(), ct);
        return ms.ToArray();
    }
}
```

**Step 4: Bygg**

```bash
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Step 5: Commit**

```bash
git add src/TronderLeikan.Infrastructure/
git commit -m "feat: implementer IDateTimeProvider og IImageProcessor med ImageSharp"
```

---

## Task 7: Infrastructure — IDomainEventDispatcher + AppDbContext.SaveChangesAsync override

AppDbContext sin `SaveChangesAsync` skal atomisk skrive: state + EventStore + Outbox i én transaksjon.

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Services/DomainEventDispatcher.cs`
- Modify: `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`

**Step 1: DomainEventDispatcher.cs** (tom implementasjon — fylt ut når RabbitMQ-consumers fins)

```csharp
using System.Text.Json;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Domain.Common;
using TronderLeikan.Infrastructure.Persistence;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Services;

// Serialiserer domenehendelser til OutboxMessages — publisering skjer via OutboxProcessor
internal sealed class DomainEventDispatcher(AppDbContext db, IDateTimeProvider clock)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        // Skriv hver hendelse som OutboxMessage — publisering skjer asynkront av OutboxProcessor
        foreach (var @event in events)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = @event.GetType().FullName!,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                CreatedAt = clock.UtcNow
            };
            db.OutboxMessages.Add(outboxMessage);
        }

        // OutboxMessages lagres i samme transaksjon som state — ikke kall SaveChanges her
        await Task.CompletedTask;
    }
}
```

**Step 2: Oppdater AppDbContext med SaveChangesAsync override**

Åpne `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`. Klassen trenger dependency på `IDateTimeProvider` og `IDomainEventDispatcher`. Erstatt klassen med:

```csharp
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            var streamId = @event switch
            {
                { } e when e.GetType().GetProperty("GameId") is { } p =>
                    p.GetValue(e)?.ToString() ?? Guid.Empty.ToString(),
                _ => Guid.Empty.ToString()
            };

            EventStore.Add(new EventStoreEntry
            {
                Id = Guid.NewGuid(),
                StreamId = streamId,
                StreamType = @event.GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                Version = 1,
                OccurredAt = clock.UtcNow
            });
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**NB:** AppDbContext har nå `IDateTimeProvider` som constructor-parameter. DependencyInjection.cs i Infrastructure må oppdateres slik at DI kan resolve dette. `IDateTimeProvider` er registrert via Infrastructure.

**Step 3: Oppdater Infrastructure.DependencyInjection.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;
using TronderLeikan.Infrastructure.Services;

namespace TronderLeikan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Registrer IDateTimeProvider først — brukes av AppDbContext
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IImageProcessor, ImageSharpImageProcessor>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Application bruker IAppDbContext — aldri AppDbContext direkte
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // IDomainEventDispatcher — ikke lenger brukt direkte (innbygd i SaveChangesAsync)
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
```

**Step 4: Bygg**

```bash
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Step 5: Commit**

```bash
git add src/TronderLeikan.Infrastructure/
git commit -m "feat: implementer SaveChangesAsync med EventStore + Outbox i AppDbContext"
```

---

## Task 8: Infrastructure — InMemoryMessagePublisher + ny EF Core migration

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Services/InMemoryMessagePublisher.cs`
- Modify: `src/TronderLeikan.Infrastructure/DependencyInjection.cs`
- Run migration

**Step 1: InMemoryMessagePublisher.cs** (stub — byttes ut med RabbitMQ-implementasjon i neste runde)

```csharp
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
```

**Step 2: Registrer IMessagePublisher i DependencyInjection.cs**

Legg til i `AddInfrastructure`:
```csharp
services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
```

**Step 3: Kjør EF Core migration**

```bash
dotnet ef migrations add AddOutboxAndEventStore \
  --project src/TronderLeikan.Infrastructure \
  --startup-project src/TronderLeikan.API
```
Forventet: Migration-fil opprettet i `src/TronderLeikan.Infrastructure/Migrations/`.

**Step 4: Bygg alt**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/TronderLeikan.Infrastructure/ src/TronderLeikan.Infrastructure/Migrations/
git commit -m "feat: legg til InMemoryMessagePublisher og EF Core migration for Outbox+EventStore"
```

---

## Task 9: DTOs (Data Transfer Objects)

Alle DTOs er enkle `record`-typer i Application-laget.

**Files:**
- Create: `src/TronderLeikan.Application/Departments/Dtos/DepartmentDto.cs`
- Create: `src/TronderLeikan.Application/Persons/Dtos/PersonSummaryDto.cs`
- Create: `src/TronderLeikan.Application/Persons/Dtos/PersonDetailDto.cs`
- Create: `src/TronderLeikan.Application/Tournaments/Dtos/TournamentSummaryDto.cs`
- Create: `src/TronderLeikan.Application/Tournaments/Dtos/TournamentDetailDto.cs`
- Create: `src/TronderLeikan.Application/Tournaments/Dtos/TournamentPointRulesDto.cs`
- Create: `src/TronderLeikan.Application/Tournaments/Dtos/ScoreboardEntryDto.cs`
- Create: `src/TronderLeikan.Application/Games/Dtos/GameDetailDto.cs`
- Create: `src/TronderLeikan.Application/Games/Dtos/SimracingResultDto.cs`

**Step 1: Alle DTOs**

`DepartmentDto.cs`:
```csharp
namespace TronderLeikan.Application.Departments.Dtos;
public record DepartmentDto(Guid Id, string Name);
```

`PersonSummaryDto.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Dtos;
public record PersonSummaryDto(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
```

`PersonDetailDto.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Dtos;
public record PersonDetailDto(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
```

`TournamentSummaryDto.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Dtos;
public record TournamentSummaryDto(Guid Id, string Name, string Slug);
```

`TournamentPointRulesDto.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Dtos;
public record TournamentPointRulesDto(
    int Participation,
    int FirstPlace,
    int SecondPlace,
    int ThirdPlace,
    int OrganizedWithParticipation,
    int OrganizedWithoutParticipation,
    int Spectator);
```

`TournamentDetailDto.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Dtos;
public record TournamentDetailDto(Guid Id, string Name, string Slug, TournamentPointRulesDto PointRules);
```

`ScoreboardEntryDto.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Dtos;
public record ScoreboardEntryDto(Guid PersonId, string FirstName, string LastName, int TotalPoints, int Rank);
```

`GameDetailDto.cs`:
```csharp
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Dtos;

public record GameDetailDto(
    Guid Id,
    Guid TournamentId,
    string Name,
    string? Description,
    bool IsDone,
    GameType GameType,
    bool HasBanner,
    bool IsOrganizersParticipating,
    IReadOnlyList<Guid> Participants,
    IReadOnlyList<Guid> Organizers,
    IReadOnlyList<Guid> Spectators,
    IReadOnlyList<Guid> FirstPlace,
    IReadOnlyList<Guid> SecondPlace,
    IReadOnlyList<Guid> ThirdPlace);
```

`SimracingResultDto.cs`:
```csharp
namespace TronderLeikan.Application.Games.Dtos;
public record SimracingResultDto(Guid Id, Guid PersonId, long RaceTimeMs);
```

**Step 2: Bygg**

```bash
dotnet build src/TronderLeikan.Application/TronderLeikan.Application.csproj
```

**Step 3: Commit**

```bash
git add src/TronderLeikan.Application/
git commit -m "feat: legg til DTO-typer for Application-laget"
```

---

## Task 10: Departments — CreateDepartmentCommand + GetDepartmentsQuery

**Files:**
- Create: `src/TronderLeikan.Application/Departments/Commands/CreateDepartment/CreateDepartmentCommand.cs`
- Create: `src/TronderLeikan.Application/Departments/Commands/CreateDepartment/CreateDepartmentCommandHandler.cs`
- Create: `src/TronderLeikan.Application/Departments/Commands/CreateDepartment/CreateDepartmentCommandValidator.cs`
- Create: `src/TronderLeikan.Application/Departments/Queries/GetDepartments/GetDepartmentsQuery.cs`
- Create: `src/TronderLeikan.Application/Departments/Queries/GetDepartments/GetDepartmentsQueryHandler.cs`
- Create: `tests/TronderLeikan.Application.Tests/Departments/CreateDepartmentCommandHandlerTests.cs`

**Step 1: Skriv failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Departments.Commands.CreateDepartment;

namespace TronderLeikan.Application.Tests.Departments;

public sealed class CreateDepartmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_GyldigNavn_ReturnererNyId()
    {
        // Arrange
        await using var db = TestAppDbContext.Create();
        var handler = new CreateDepartmentCommandHandler(db);
        var command = new CreateDepartmentCommand("IT");

        // Act
        var result = await handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        var dept = await db.Departments.FindAsync(result.Value);
        Assert.NotNull(dept);
        Assert.Equal("IT", dept.Name);
    }

    [Fact]
    public async Task Handle_TomtNavn_ReturnererFeil()
    {
        // Arrange
        await using var db = TestAppDbContext.Create();
        var handler = new CreateDepartmentCommandHandler(db);
        var command = new CreateDepartmentCommand("");

        // Act
        var result = await handler.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
```

**Step 2: Kjør test — verifiser at de feiler**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~CreateDepartmentCommandHandlerTests"
```
Forventet: FAIL (types not found).

**Step 3: Implementer CreateDepartmentCommand.cs**

```csharp
namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;
public record CreateDepartmentCommand(string Name);
```

**Step 4: CreateDepartmentCommandHandler.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Departments;

namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Guid>.Fail("Navn kan ikke være tomt.");

        var department = Department.Create(command.Name);
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(department.Id);
    }
}
```

**Step 5: CreateDepartmentCommandValidator.cs**

```csharp
using FluentValidation;

namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Avdelingsnavn kan ikke være tomt.")
            .MaximumLength(200).WithMessage("Avdelingsnavn kan ikke overstige 200 tegn.");
    }
}
```

**Step 6: GetDepartmentsQuery.cs**

```csharp
namespace TronderLeikan.Application.Departments.Queries.GetDepartments;
public record GetDepartmentsQuery;
```

**Step 7: GetDepartmentsQueryHandler.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Departments.Dtos;

namespace TronderLeikan.Application.Departments.Queries.GetDepartments;

public sealed class GetDepartmentsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetDepartmentsQuery, DepartmentDto[]>
{
    public async Task<Result<DepartmentDto[]>> Handle(GetDepartmentsQuery query, CancellationToken ct = default)
    {
        var departments = await db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name))
            .ToArrayAsync(ct);
        return Result<DepartmentDto[]>.Ok(departments);
    }
}
```

**Step 8: Kjør test — verifiser at de passerer**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~CreateDepartmentCommandHandlerTests"
```
Forventet: PASS 2/2.

**Step 9: Bygg alt**

```bash
dotnet build
```

**Step 10: Commit**

```bash
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer CreateDepartmentCommand og GetDepartmentsQuery med tester"
```

---

## Task 11: Persons — CreatePerson, UpdatePerson, DeletePerson

**Files:**
- Create: `src/TronderLeikan.Application/Persons/Commands/CreatePerson/CreatePersonCommand.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/CreatePerson/CreatePersonCommandHandler.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/CreatePerson/CreatePersonCommandValidator.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/UpdatePerson/UpdatePersonCommand.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/UpdatePerson/UpdatePersonCommandHandler.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/DeletePerson/DeletePersonCommand.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/DeletePerson/DeletePersonCommandHandler.cs`
- Create: `tests/TronderLeikan.Application.Tests/Persons/PersonCommandHandlerTests.cs`

**Step 1: Skriv failing tester**

```csharp
using TronderLeikan.Application.Persons.Commands.CreatePerson;
using TronderLeikan.Application.Persons.Commands.UpdatePerson;
using TronderLeikan.Application.Persons.Commands.DeletePerson;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonCommandHandlerTests
{
    [Fact]
    public async Task CreatePerson_GyldigInput_LagrerPerson()
    {
        await using var db = TestAppDbContext.Create();
        var handler = new CreatePersonCommandHandler(db);
        var result = await handler.Handle(new CreatePersonCommand("Ola", "Nordmann", null));
        Assert.True(result.IsSuccess);
        Assert.Single(db.Persons.ToList());
    }

    [Fact]
    public async Task UpdatePerson_EksisterendePerson_OppdatererNavn()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new UpdatePersonCommandHandler(db);
        var result = await handler.Handle(new UpdatePersonCommand(person.Id, "Kari", "Nordmann", null));

        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.Equal("Kari", updated!.FirstName);
    }

    [Fact]
    public async Task DeletePerson_EksisterendePerson_FjenerPerson()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new DeletePersonCommandHandler(db);
        var result = await handler.Handle(new DeletePersonCommand(person.Id));

        Assert.True(result.IsSuccess);
        Assert.Empty(db.Persons.ToList());
    }

    [Fact]
    public async Task DeletePerson_IkkeEksisterende_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var handler = new DeletePersonCommandHandler(db);
        var result = await handler.Handle(new DeletePersonCommand(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
    }
}
```

**Step 2: Kjør — verifiser at de feiler**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~PersonCommandHandlerTests"
```

**Step 3: Implementer alle kommandoer**

`CreatePersonCommand.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Commands.CreatePerson;
public record CreatePersonCommand(string FirstName, string LastName, Guid? DepartmentId);
```

`CreatePersonCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Persons.Commands.CreatePerson;

public sealed class CreatePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<CreatePersonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePersonCommand command, CancellationToken ct = default)
    {
        var person = Person.Create(command.FirstName, command.LastName, command.DepartmentId);
        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(person.Id);
    }
}
```

`CreatePersonCommandValidator.cs`:
```csharp
using FluentValidation;

namespace TronderLeikan.Application.Persons.Commands.CreatePerson;

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(200);
    }
}
```

`UpdatePersonCommand.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Commands.UpdatePerson;
public record UpdatePersonCommand(Guid PersonId, string FirstName, string LastName, Guid? DepartmentId);
```

`UpdatePersonCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.UpdatePerson;

public sealed class UpdatePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdatePersonCommand>
{
    public async Task<Result> Handle(UpdatePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        // Person har ikke UpdateName-metode — setter properties via reflection er feil.
        // Bruk domene-metoder. Person.Create er for nye personer.
        // Vi må legge til UpdateName og UpdateDepartment i Person — men NB: domenemodellen
        // fins allerede. Sjekk Person.cs: den har UpdateDepartment men ikke UpdateName.
        // Løsning: Legg til void Update(firstName, lastName) i Person eller bruk EF shadow props.
        // ENKLESTE: Legg til Update-metode i Person (domenemodellen).
        // Se Task 11, Note: Person-entiteten trenger en Update-metode.
        // Midlertidig: bruk backing field via reflection (IKKE ok i prod)
        // RIKTIG: oppdater domenemodellen Person.cs med:
        //   public void Update(string firstName, string lastName) { FirstName = firstName; LastName = lastName; }
        // Dette er et domene-valg, ikke et Application-valg.
        // GJØR: Legg til Update-metode i Person.cs (src/TronderLeikan.Domain/Persons/Person.cs)
        // Deretter: person.Update(command.FirstName, command.LastName);
        person.UpdateDepartment(command.DepartmentId);
        // TODO etter domain update: person.Update(command.FirstName, command.LastName);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

> **Viktig:** Åpne `src/TronderLeikan.Domain/Persons/Person.cs` og legg til:
> ```csharp
> public void Update(string firstName, string lastName)
> {
>     FirstName = firstName;
>     LastName = lastName;
> }
> ```
> Og erstatt TODO-linja i handler med `person.Update(command.FirstName, command.LastName);`

`DeletePersonCommand.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Commands.DeletePerson;
public record DeletePersonCommand(Guid PersonId);
```

`DeletePersonCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePerson;

public sealed class DeletePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonCommand>
{
    public async Task<Result> Handle(DeletePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        db.Persons.Remove(person);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

**Step 4: Kjør tester**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~PersonCommandHandlerTests"
```
Forventet: PASS 4/4.

**Step 5: Commit**

```bash
git add src/TronderLeikan.Domain/ src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer Person-kommandoer (Create, Update, Delete) med tester"
```

---

## Task 12: Persons — UploadPersonImage + DeletePersonImage

**Files:**
- Create: `src/TronderLeikan.Application/Persons/Commands/UploadPersonImage/UploadPersonImageCommand.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/UploadPersonImage/UploadPersonImageCommandHandler.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/DeletePersonImage/DeletePersonImageCommand.cs`
- Create: `src/TronderLeikan.Application/Persons/Commands/DeletePersonImage/DeletePersonImageCommandHandler.cs`
- Create: `tests/TronderLeikan.Application.Tests/Persons/PersonImageCommandHandlerTests.cs`

**Step 1: Skriv failing tester**

```csharp
using TronderLeikan.Application.Persons.Commands.UploadPersonImage;
using TronderLeikan.Application.Persons.Commands.DeletePersonImage;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonImageCommandHandlerTests
{
    // Enkel test-double for IImageProcessor
    private sealed class FakeImageProcessor : TronderLeikan.Application.Common.Interfaces.IImageProcessor
    {
        public Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(new byte[] { 1, 2, 3 });
        public Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(new byte[] { 4, 5, 6 });
    }

    [Fact]
    public async Task UploadPersonImage_LagrerBildeOgSetterFlag()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new UploadPersonImageCommandHandler(db, new FakeImageProcessor());
        using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
        var result = await handler.Handle(new UploadPersonImageCommand(person.Id, stream));

        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.True(updated!.HasProfileImage);
        var image = await db.PersonImages.FindAsync(person.Id);
        Assert.NotNull(image);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.ImageData);
    }

    [Fact]
    public async Task DeletePersonImage_FjenerBildeOgClearerFlag()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        person.SetProfileImage();
        db.PersonImages.Add(new PersonImage { PersonId = person.Id, ImageData = [1], ContentType = "image/webp" });
        await db.SaveChangesAsync();

        var handler = new DeletePersonImageCommandHandler(db);
        var result = await handler.Handle(new DeletePersonImageCommand(person.Id));

        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.False(updated!.HasProfileImage);
    }
}
```

**Step 2: Implementer kommandoer**

`UploadPersonImageCommand.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Commands.UploadPersonImage;
public record UploadPersonImageCommand(Guid PersonId, Stream ImageStream);
```

`UploadPersonImageCommandHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Application.Persons.Commands.UploadPersonImage;

public sealed class UploadPersonImageCommandHandler(IAppDbContext db, IImageProcessor imageProcessor)
    : ICommandHandler<UploadPersonImageCommand>
{
    public async Task<Result> Handle(UploadPersonImageCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        // Konverter til WebP via IImageProcessor — Application vet ikke om ImageSharp
        var processedBytes = await imageProcessor.ProcessPersonImageAsync(command.ImageStream, ct);

        // Upsert — erstatt eksisterende bilde om det fins
        var existing = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (existing is not null)
        {
            existing.ImageData = processedBytes;
        }
        else
        {
            db.PersonImages.Add(new PersonImage
            {
                PersonId = command.PersonId,
                ImageData = processedBytes,
                ContentType = "image/webp"
            });
        }

        person.SetProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`DeletePersonImageCommand.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Commands.DeletePersonImage;
public record DeletePersonImageCommand(Guid PersonId);
```

`DeletePersonImageCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePersonImage;

public sealed class DeletePersonImageCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonImageCommand>
{
    public async Task<Result> Handle(DeletePersonImageCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        var image = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (image is not null)
            db.PersonImages.Remove(image);

        person.RemoveProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~PersonImageCommandHandlerTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer bildeopplasting og -sletting for Person"
```

---

## Task 13: Persons Queries — GetPersons + GetPersonById

**Files:**
- Create: `src/TronderLeikan.Application/Persons/Queries/GetPersons/GetPersonsQuery.cs`
- Create: `src/TronderLeikan.Application/Persons/Queries/GetPersons/GetPersonsQueryHandler.cs`
- Create: `src/TronderLeikan.Application/Persons/Queries/GetPersonById/GetPersonByIdQuery.cs`
- Create: `src/TronderLeikan.Application/Persons/Queries/GetPersonById/GetPersonByIdQueryHandler.cs`
- Create: `tests/TronderLeikan.Application.Tests/Persons/PersonQueryHandlerTests.cs`

**Step 1: Tester**

```csharp
using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonQueryHandlerTests
{
    [Fact]
    public async Task GetPersons_ReturnererAllePersoner()
    {
        await using var db = TestAppDbContext.Create();
        db.Persons.AddRange(Person.Create("Ola", "Nordmann"), Person.Create("Kari", "Traa"));
        await db.SaveChangesAsync();

        var result = await new GetPersonsQueryHandler(db).Handle(new GetPersonsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Length);
    }

    [Fact]
    public async Task GetPersonById_EksisterendePerson_ReturnererDetaljer()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(person.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ola", result.Value!.FirstName);
    }

    [Fact]
    public async Task GetPersonById_IkkeEksisterende_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
    }
}
```

**Step 2: Implementer**

`GetPersonsQuery.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Queries.GetPersons;
public record GetPersonsQuery;
```

`GetPersonsQueryHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Dtos;

namespace TronderLeikan.Application.Persons.Queries.GetPersons;

public sealed class GetPersonsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonsQuery, PersonSummaryDto[]>
{
    public async Task<Result<PersonSummaryDto[]>> Handle(GetPersonsQuery query, CancellationToken ct = default)
    {
        var persons = await db.Persons
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PersonSummaryDto(p.Id, p.FirstName, p.LastName, p.DepartmentId, p.HasProfileImage))
            .ToArrayAsync(ct);
        return Result<PersonSummaryDto[]>.Ok(persons);
    }
}
```

`GetPersonByIdQuery.cs`:
```csharp
namespace TronderLeikan.Application.Persons.Queries.GetPersonById;
public record GetPersonByIdQuery(Guid PersonId);
```

`GetPersonByIdQueryHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Dtos;

namespace TronderLeikan.Application.Persons.Queries.GetPersonById;

public sealed class GetPersonByIdQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonByIdQuery, PersonDetailDto>
{
    public async Task<Result<PersonDetailDto>> Handle(GetPersonByIdQuery query, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([query.PersonId], ct);
        if (person is null)
            return Result<PersonDetailDto>.Fail($"Person med Id {query.PersonId} finnes ikke.");

        return Result<PersonDetailDto>.Ok(
            new PersonDetailDto(person.Id, person.FirstName, person.LastName, person.DepartmentId, person.HasProfileImage));
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~PersonQueryHandlerTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer GetPersons og GetPersonById queries"
```

---

## Task 14: Tournaments — CreateTournament + UpdateTournamentPointRules

**Files:**
- Create: `src/TronderLeikan.Application/Tournaments/Commands/CreateTournament/` (Command + Handler + Validator)
- Create: `src/TronderLeikan.Application/Tournaments/Commands/UpdateTournamentPointRules/` (Command + Handler)
- Create: `tests/TronderLeikan.Application.Tests/Tournaments/TournamentCommandHandlerTests.cs`

**Step 1: Tester**

```csharp
using TronderLeikan.Application.Tournaments.Commands.CreateTournament;
using TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Tournaments;

public sealed class TournamentCommandHandlerTests
{
    [Fact]
    public async Task CreateTournament_GyldigInput_LagrerTurnering()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new CreateTournamentCommandHandler(db)
            .Handle(new CreateTournamentCommand("NM 2026", "nm-2026"));
        Assert.True(result.IsSuccess);
        Assert.Single(db.Tournaments.ToList());
    }

    [Fact]
    public async Task UpdateTournamentPointRules_OppdatererRegler()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM 2026", "nm-2026");
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var command = new UpdateTournamentPointRulesCommand(tournament.Id, 5, 4, 3, 2, 2, 4, 1);
        var result = await new UpdateTournamentPointRulesCommandHandler(db).Handle(command);

        Assert.True(result.IsSuccess);
        var updated = await db.Tournaments.FindAsync(tournament.Id);
        Assert.Equal(5, updated!.PointRules.Participation);
        Assert.Equal(4, updated.PointRules.FirstPlace);
    }
}
```

**Step 2: Implementer**

`CreateTournamentCommand.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;
public record CreateTournamentCommand(string Name, string Slug);
```

`CreateTournamentCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;

public sealed class CreateTournamentCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateTournamentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTournamentCommand command, CancellationToken ct = default)
    {
        var tournament = Tournament.Create(command.Name, command.Slug);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(tournament.Id);
    }
}
```

`CreateTournamentCommandValidator.cs`:
```csharp
using FluentValidation;

namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;

public sealed class CreateTournamentCommandValidator : AbstractValidator<CreateTournamentCommand>
{
    public CreateTournamentCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug kan kun inneholde små bokstaver, tall og bindestrek.");
    }
}
```

`UpdateTournamentPointRulesCommand.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;

public record UpdateTournamentPointRulesCommand(
    Guid TournamentId,
    int Participation,
    int FirstPlace,
    int SecondPlace,
    int ThirdPlace,
    int OrganizedWithParticipation,
    int OrganizedWithoutParticipation,
    int Spectator);
```

`UpdateTournamentPointRulesCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;

public sealed class UpdateTournamentPointRulesCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdateTournamentPointRulesCommand>
{
    public async Task<Result> Handle(UpdateTournamentPointRulesCommand command, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([command.TournamentId], ct);
        if (tournament is null)
            return Result.Fail($"Turnering med Id {command.TournamentId} finnes ikke.");

        tournament.UpdatePointRules(TournamentPointRules.Custom(
            command.Participation,
            command.FirstPlace,
            command.SecondPlace,
            command.ThirdPlace,
            command.OrganizedWithParticipation,
            command.OrganizedWithoutParticipation,
            command.Spectator));

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~TournamentCommandHandlerTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer Tournament-kommandoer med tester"
```

---

## Task 15: Tournament Queries — GetTournaments, GetTournamentBySlug, GetScoreboard

GetScoreboard er den mest komplekse: beregner poeng per person på tvers av alle `IsDone`-spill.

**Files:**
- Create: `src/TronderLeikan.Application/Tournaments/Queries/GetTournaments/` (Query + Handler)
- Create: `src/TronderLeikan.Application/Tournaments/Queries/GetTournamentBySlug/` (Query + Handler)
- Create: `src/TronderLeikan.Application/Tournaments/Queries/GetScoreboard/` (Query + Handler)
- Create: `tests/TronderLeikan.Application.Tests/Tournaments/TournamentQueryHandlerTests.cs`

**Step 1: Tester (inkludert scoreboard-logikk)**

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Tournaments.Queries.GetTournaments;
using TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
using TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Tournaments;

public sealed class TournamentQueryHandlerTests
{
    [Fact]
    public async Task GetTournaments_ReturnererAlleturneringer()
    {
        await using var db = TestAppDbContext.Create();
        db.Tournaments.Add(Tournament.Create("NM 2026", "nm-2026"));
        await db.SaveChangesAsync();
        var result = await new GetTournamentsQueryHandler(db).Handle(new GetTournamentsQuery());
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetTournamentBySlug_FinnesTournering()
    {
        await using var db = TestAppDbContext.Create();
        db.Tournaments.Add(Tournament.Create("NM 2026", "nm-2026"));
        await db.SaveChangesAsync();
        var result = await new GetTournamentBySlugQueryHandler(db).Handle(new GetTournamentBySlugQuery("nm-2026"));
        Assert.True(result.IsSuccess);
        Assert.Equal("NM 2026", result.Value!.Name);
    }

    [Fact]
    public async Task GetScoreboard_BeregnerPoengRiktig()
    {
        // Arrange: turnering, 2 spillere, 1 fullført spill
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        db.Tournaments.Add(tournament);

        var personOla = Person.Create("Ola", "Nordmann");
        var personKari = Person.Create("Kari", "Traa");
        db.Persons.AddRange(personOla, personKari);

        // Spill med Ola som 1. plass og Kari som deltaker
        var game = Game.Create("Spill 1", tournament.Id);
        game.AddParticipant(personOla.Id);
        game.AddParticipant(personKari.Id);
        game.Complete([personOla.Id], [personKari.Id], []);
        db.Games.Add(game);

        await db.SaveChangesAsync();

        // Act
        var result = await new GetScoreboardQueryHandler(db).Handle(new GetScoreboardQuery(tournament.Id));

        // Assert: Ola: participation(3) + firstPlace(3) = 6, Kari: participation(3) + secondPlace(2) = 5
        Assert.True(result.IsSuccess);
        var entries = result.Value!;
        var ola = entries.Single(e => e.PersonId == personOla.Id);
        var kari = entries.Single(e => e.PersonId == personKari.Id);
        Assert.Equal(6, ola.TotalPoints);
        Assert.Equal(5, kari.TotalPoints);
        Assert.Equal(1, ola.Rank);
        Assert.Equal(2, kari.Rank);
    }
}
```

**Step 2: Implementer GetTournaments + GetTournamentBySlug**

`GetTournamentsQuery.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Queries.GetTournaments;
public record GetTournamentsQuery;
```

`GetTournamentsQueryHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Dtos;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournaments;

public sealed class GetTournamentsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentsQuery, TournamentSummaryDto[]>
{
    public async Task<Result<TournamentSummaryDto[]>> Handle(GetTournamentsQuery query, CancellationToken ct = default)
    {
        var tournaments = await db.Tournaments
            .OrderBy(t => t.Name)
            .Select(t => new TournamentSummaryDto(t.Id, t.Name, t.Slug))
            .ToArrayAsync(ct);
        return Result<TournamentSummaryDto[]>.Ok(tournaments);
    }
}
```

`GetTournamentBySlugQuery.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
public record GetTournamentBySlugQuery(string Slug);
```

`GetTournamentBySlugQueryHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Dtos;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;

public sealed class GetTournamentBySlugQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentBySlugQuery, TournamentDetailDto>
{
    public async Task<Result<TournamentDetailDto>> Handle(GetTournamentBySlugQuery query, CancellationToken ct = default)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(t => t.Slug == query.Slug, ct);
        if (t is null)
            return Result<TournamentDetailDto>.Fail($"Turnering med slug '{query.Slug}' finnes ikke.");

        var rules = new TournamentPointRulesDto(
            t.PointRules.Participation, t.PointRules.FirstPlace, t.PointRules.SecondPlace,
            t.PointRules.ThirdPlace, t.PointRules.OrganizedWithParticipation,
            t.PointRules.OrganizedWithoutParticipation, t.PointRules.Spectator);
        return Result<TournamentDetailDto>.Ok(new TournamentDetailDto(t.Id, t.Name, t.Slug, rules));
    }
}
```

**Step 3: Implementer GetScoreboardQueryHandler**

Scoreboard-logikk: for hvert `IsDone`-spill, akkumuler poeng per person.

`GetScoreboardQuery.cs`:
```csharp
namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
public record GetScoreboardQuery(Guid TournamentId);
```

`GetScoreboardQueryHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Dtos;

namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;

public sealed class GetScoreboardQueryHandler(IAppDbContext db)
    : IQueryHandler<GetScoreboardQuery, ScoreboardEntryDto[]>
{
    public async Task<Result<ScoreboardEntryDto[]>> Handle(GetScoreboardQuery query, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([query.TournamentId], ct);
        if (tournament is null)
            return Result<ScoreboardEntryDto[]>.Fail($"Turnering med Id {query.TournamentId} finnes ikke.");

        var rules = tournament.PointRules;

        // Hent alle fullførte spill i turneringen
        var games = await db.Games
            .Where(g => g.TournamentId == query.TournamentId && g.IsDone)
            .ToListAsync(ct);

        // Hent alle involverte personer for å slå opp navn
        var allPersonIds = games.SelectMany(g =>
            g.Participants.Concat(g.Organizers).Concat(g.Spectators)).Distinct().ToList();

        var persons = await db.Persons
            .Where(p => allPersonIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Akkumuler poeng per person
        var points = new Dictionary<Guid, int>();

        foreach (var game in games)
        {
            // Deltakere
            foreach (var personId in game.Participants)
                points[personId] = points.GetValueOrDefault(personId) + rules.Participation;

            // Arrangører med deltakelse
            foreach (var personId in game.Organizers)
            {
                points[personId] = game.IsOrganizersParticipating
                    ? points.GetValueOrDefault(personId) + rules.OrganizedWithParticipation + rules.Participation
                    : points.GetValueOrDefault(personId) + rules.OrganizedWithoutParticipation;
            }

            // Tilskuere
            foreach (var personId in game.Spectators)
                points[personId] = points.GetValueOrDefault(personId) + rules.Spectator;

            // Plasseringer er additive (legges oppå deltakelsespoengene)
            foreach (var personId in game.FirstPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.FirstPlace;
            foreach (var personId in game.SecondPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.SecondPlace;
            foreach (var personId in game.ThirdPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.ThirdPlace;
        }

        // Sorter synkende på poeng, beregn rank med ties
        var sorted = points
            .OrderByDescending(kv => kv.Value)
            .ToList();

        var entries = new List<ScoreboardEntryDto>();
        var rank = 1;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (i > 0 && sorted[i].Value < sorted[i - 1].Value)
                rank = i + 1;

            var personId = sorted[i].Key;
            if (!persons.TryGetValue(personId, out var person))
                continue;

            entries.Add(new ScoreboardEntryDto(personId, person.FirstName, person.LastName, sorted[i].Value, rank));
        }

        return Result<ScoreboardEntryDto[]>.Ok([.. entries]);
    }
}
```

**Step 4: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~TournamentQueryHandlerTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer Tournament-queries inkludert GetScoreboard"
```

---

## Task 16: Games — Create, Update, AddParticipant/Organizer/Spectator, CompleteGame

**Files:**
- Create: `src/TronderLeikan.Application/Games/Commands/CreateGame/` (Command + Handler + Validator)
- Create: `src/TronderLeikan.Application/Games/Commands/UpdateGame/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Commands/AddParticipant/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Commands/AddOrganizer/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Commands/AddSpectator/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Commands/CompleteGame/` (Command + Handler)
- Create: `tests/TronderLeikan.Application.Tests/Games/GameCommandHandlerTests.cs`

**Step 1: Tester**

```csharp
using TronderLeikan.Application.Games.Commands.CreateGame;
using TronderLeikan.Application.Games.Commands.UpdateGame;
using TronderLeikan.Application.Games.Commands.AddParticipant;
using TronderLeikan.Application.Games.Commands.AddOrganizer;
using TronderLeikan.Application.Games.Commands.AddSpectator;
using TronderLeikan.Application.Games.Commands.CompleteGame;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Games;

public sealed class GameCommandHandlerTests
{
    [Fact]
    public async Task CreateGame_LagrerSpill()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var result = await new CreateGameCommandHandler(db)
            .Handle(new CreateGameCommand(tournament.Id, "Dartspill", GameType.Standard));

        Assert.True(result.IsSuccess);
        Assert.Single(db.Games.ToList());
    }

    [Fact]
    public async Task AddParticipant_LeggerTilDeltaker()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var result = await new AddParticipantCommandHandler(db)
            .Handle(new AddParticipantCommand(game.Id, person.Id));

        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.Contains(person.Id, updated!.Participants);
    }

    [Fact]
    public async Task CompleteGame_SetterIsDoneOgPlasseringer()
    {
        await using var db = TestAppDbContext.Create();
        var personA = Person.Create("A", "A");
        var personB = Person.Create("B", "B");
        db.Persons.AddRange(personA, personB);
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var result = await new CompleteGameCommandHandler(db)
            .Handle(new CompleteGameCommand(game.Id, [personA.Id], [personB.Id], []));

        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.True(updated!.IsDone);
        Assert.Contains(personA.Id, updated.FirstPlace);
    }
}
```

**Step 2: Implementer alle kommandoer**

`CreateGameCommand.cs`:
```csharp
using TronderLeikan.Domain.Games;
namespace TronderLeikan.Application.Games.Commands.CreateGame;
public record CreateGameCommand(Guid TournamentId, string Name, GameType GameType);
```

`CreateGameCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Commands.CreateGame;

public sealed class CreateGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateGameCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGameCommand command, CancellationToken ct = default)
    {
        var game = Game.Create(command.Name, command.TournamentId, command.GameType);
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(game.Id);
    }
}
```

`CreateGameCommandValidator.cs`:
```csharp
using FluentValidation;
namespace TronderLeikan.Application.Games.Commands.CreateGame;
public sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(500);
        RuleFor(c => c.TournamentId).NotEmpty();
    }
}
```

`UpdateGameCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.UpdateGame;
public record UpdateGameCommand(Guid GameId, string Name, string? Description);
```

`UpdateGameCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.UpdateGame;

public sealed class UpdateGameCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdateGameCommand>
{
    public async Task<Result> Handle(UpdateGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null)
            return Result.Fail($"Spill med Id {command.GameId} finnes ikke.");

        // Game.cs mangler UpdateName — må legges til i domenet
        // Legg til i Game.cs: public void UpdateName(string name) => Name = name;
        game.UpdateDescription(command.Description);
        // TODO etter domain update: game.UpdateName(command.Name);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

> **Viktig:** Legg til `public void UpdateName(string name) => Name = name;` i `src/TronderLeikan.Domain/Games/Game.cs`

`AddParticipantCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.AddParticipant;
public record AddParticipantCommand(Guid GameId, Guid PersonId);
```

`AddParticipantCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler(IAppDbContext db)
    : ICommandHandler<AddParticipantCommand>
{
    public async Task<Result> Handle(AddParticipantCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        game.AddParticipant(command.PersonId);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`AddOrganizerCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.AddOrganizer;
public record AddOrganizerCommand(Guid GameId, Guid PersonId, bool WithParticipation);
```

`AddOrganizerCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddOrganizer;

public sealed class AddOrganizerCommandHandler(IAppDbContext db)
    : ICommandHandler<AddOrganizerCommand>
{
    public async Task<Result> Handle(AddOrganizerCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        game.AddOrganizer(command.PersonId, command.WithParticipation);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`AddSpectatorCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.AddSpectator;
public record AddSpectatorCommand(Guid GameId, Guid PersonId);
```

`AddSpectatorCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.AddSpectator;

public sealed class AddSpectatorCommandHandler(IAppDbContext db)
    : ICommandHandler<AddSpectatorCommand>
{
    public async Task<Result> Handle(AddSpectatorCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        game.AddSpectator(command.PersonId);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`CompleteGameCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.CompleteGame;
public record CompleteGameCommand(Guid GameId, Guid[] FirstPlace, Guid[] SecondPlace, Guid[] ThirdPlace);
```

`CompleteGameCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.CompleteGame;

public sealed class CompleteGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CompleteGameCommand>
{
    public async Task<Result> Handle(CompleteGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        if (game.IsDone) return Result.Fail("Spillet er allerede fullført.");

        game.Complete(command.FirstPlace, command.SecondPlace, command.ThirdPlace);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~GameCommandHandlerTests"
git add src/TronderLeikan.Domain/ src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer Game-kommandoer (Create, Update, Add*, CompleteGame)"
```

---

## Task 17: Games — UploadGameBanner + GetGameById

**Files:**
- Create: `src/TronderLeikan.Application/Games/Commands/UploadGameBanner/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Queries/GetGameById/` (Query + Handler)
- Create: `tests/TronderLeikan.Application.Tests/Games/GameBannerAndQueryTests.cs`

**Step 1: Tester**

```csharp
using TronderLeikan.Application.Games.Commands.UploadGameBanner;
using TronderLeikan.Application.Games.Queries.GetGameById;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Tests.Games;

public sealed class GameBannerAndQueryTests
{
    private sealed class FakeImageProcessor : TronderLeikan.Application.Common.Interfaces.IImageProcessor
    {
        public Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(new byte[] { 1, 2 });
    }

    [Fact]
    public async Task UploadGameBanner_LagrerBannerOgSetterFlag()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();

        using var stream = new MemoryStream([9]);
        var result = await new UploadGameBannerCommandHandler(db, new FakeImageProcessor())
            .Handle(new UploadGameBannerCommand(game.Id, stream));

        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.True(updated!.HasBanner);
    }

    [Fact]
    public async Task GetGameById_ReturnererSpillDetaljer()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var result = await new GetGameByIdQueryHandler(db).Handle(new GetGameByIdQuery(game.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Spill", result.Value!.Name);
    }
}
```

**Step 2: Implementer**

`UploadGameBannerCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.UploadGameBanner;
public record UploadGameBannerCommand(Guid GameId, Stream BannerStream);
```

`UploadGameBannerCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Application.Games.Commands.UploadGameBanner;

public sealed class UploadGameBannerCommandHandler(IAppDbContext db, IImageProcessor imageProcessor)
    : ICommandHandler<UploadGameBannerCommand>
{
    public async Task<Result> Handle(UploadGameBannerCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");

        var bytes = await imageProcessor.ProcessGameBannerAsync(command.BannerStream, ct);

        var existing = await db.GameBanners.FindAsync([command.GameId], ct);
        if (existing is not null)
        {
            existing.ImageData = bytes;
        }
        else
        {
            db.GameBanners.Add(new GameBanner
            {
                GameId = command.GameId,
                ImageData = bytes,
                ContentType = "image/webp"
            });
        }

        game.SetBanner();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`GetGameByIdQuery.cs`:
```csharp
namespace TronderLeikan.Application.Games.Queries.GetGameById;
public record GetGameByIdQuery(Guid GameId);
```

`GetGameByIdQueryHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Games.Dtos;

namespace TronderLeikan.Application.Games.Queries.GetGameById;

public sealed class GetGameByIdQueryHandler(IAppDbContext db)
    : IQueryHandler<GetGameByIdQuery, GameDetailDto>
{
    public async Task<Result<GameDetailDto>> Handle(GetGameByIdQuery query, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([query.GameId], ct);
        if (game is null) return Result<GameDetailDto>.Fail($"Spill {query.GameId} finnes ikke.");

        return Result<GameDetailDto>.Ok(new GameDetailDto(
            game.Id, game.TournamentId, game.Name, game.Description,
            game.IsDone, game.GameType, game.HasBanner, game.IsOrganizersParticipating,
            game.Participants, game.Organizers, game.Spectators,
            game.FirstPlace, game.SecondPlace, game.ThirdPlace));
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~GameBannerAndQueryTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer UploadGameBanner og GetGameById"
```

---

## Task 18: Simracing — RegisterSimracingResult + CompleteSimracingGame + GetSimracingResults

**Files:**
- Create: `src/TronderLeikan.Application/Games/Commands/RegisterSimracingResult/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Commands/CompleteSimracingGame/` (Command + Handler)
- Create: `src/TronderLeikan.Application/Games/Queries/GetSimracingResults/` (Query + Handler)
- Create: `tests/TronderLeikan.Application.Tests/Games/SimracingHandlerTests.cs`

**Step 1: Tester**

```csharp
using TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
using TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
using TronderLeikan.Application.Games.Queries.GetSimracingResults;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Games;

public sealed class SimracingHandlerTests
{
    [Fact]
    public async Task RegisterSimracingResult_LagrerResultat()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var person = Person.Create("Ola", "Nordmann");
        db.Games.Add(game);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var result = await new RegisterSimracingResultCommandHandler(db)
            .Handle(new RegisterSimracingResultCommand(game.Id, person.Id, 93500L));

        Assert.True(result.IsSuccess);
        Assert.Single(db.SimracingResults.ToList());
    }

    [Fact]
    public async Task CompleteSimracingGame_BeregnerPlasseringerFraRacetider()
    {
        // Arrange: 3 spillere med ulike tider
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var personA = Person.Create("A", "A"); // raskest
        var personB = Person.Create("B", "B"); // tregeste
        var personC = Person.Create("C", "C"); // midten
        db.Games.Add(game);
        db.Persons.AddRange(personA, personB, personC);

        // Legg til racetider: A=90000, B=95000, C=92000
        db.SimracingResults.AddRange(
            SimracingResult.Register(game.Id, personA.Id, 90000L),
            SimracingResult.Register(game.Id, personB.Id, 95000L),
            SimracingResult.Register(game.Id, personC.Id, 92000L));
        await db.SaveChangesAsync();

        // Act
        var result = await new CompleteSimracingGameCommandHandler(db)
            .Handle(new CompleteSimracingGameCommand(game.Id));

        // Assert: A = 1. plass, C = 2. plass, B = 3. plass
        Assert.True(result.IsSuccess);
        var completed = await db.Games.FindAsync(game.Id);
        Assert.True(completed!.IsDone);
        Assert.Contains(personA.Id, completed.FirstPlace);
        Assert.Contains(personC.Id, completed.SecondPlace);
        Assert.Contains(personB.Id, completed.ThirdPlace);
    }

    [Fact]
    public async Task CompleteSimracingGame_Ties_DelerPlassering()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("F1 Race", Guid.NewGuid(), GameType.Simracing);
        var personA = Person.Create("A", "A");
        var personB = Person.Create("B", "B");
        db.Games.Add(game);
        db.Persons.AddRange(personA, personB);

        // Begge har samme tid — deler 1. plass
        db.SimracingResults.AddRange(
            SimracingResult.Register(game.Id, personA.Id, 90000L),
            SimracingResult.Register(game.Id, personB.Id, 90000L));
        await db.SaveChangesAsync();

        var result = await new CompleteSimracingGameCommandHandler(db)
            .Handle(new CompleteSimracingGameCommand(game.Id));

        Assert.True(result.IsSuccess);
        var completed = await db.Games.FindAsync(game.Id);
        Assert.Contains(personA.Id, completed!.FirstPlace);
        Assert.Contains(personB.Id, completed.FirstPlace);
        Assert.Empty(completed.SecondPlace);
    }
}
```

**Step 2: Implementer**

`RegisterSimracingResultCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
public record RegisterSimracingResultCommand(Guid GameId, Guid PersonId, long RaceTimeMs);
```

`RegisterSimracingResultCommandHandler.cs`:
```csharp
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Commands.RegisterSimracingResult;

public sealed class RegisterSimracingResultCommandHandler(IAppDbContext db)
    : ICommandHandler<RegisterSimracingResultCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterSimracingResultCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result<Guid>.Fail($"Spill {command.GameId} finnes ikke.");
        if (game.IsDone) return Result<Guid>.Fail("Spillet er allerede fullført.");

        var result = SimracingResult.Register(command.GameId, command.PersonId, command.RaceTimeMs);
        db.SimracingResults.Add(result);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(result.Id);
    }
}
```

`CompleteSimracingGameCommand.cs`:
```csharp
namespace TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
public record CompleteSimracingGameCommand(Guid GameId);
```

`CompleteSimracingGameCommandHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Games.Commands.CompleteSimracingGame;

public sealed class CompleteSimracingGameCommandHandler(IAppDbContext db)
    : ICommandHandler<CompleteSimracingGameCommand>
{
    public async Task<Result> Handle(CompleteSimracingGameCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");
        if (game.IsDone) return Result.Fail("Spillet er allerede fullført.");

        // Hent alle resultater, sorter på racetid ascending (lavest = raskest = best)
        var results = await db.SimracingResults
            .Where(r => r.GameId == command.GameId)
            .OrderBy(r => r.RaceTimeMs)
            .ToListAsync(ct);

        if (results.Count == 0)
            return Result.Fail("Ingen racetider registrert for dette spillet.");

        // Grupper tider som er like — disse deler plassering (ties)
        var groups = results
            .GroupBy(r => r.RaceTimeMs)
            .OrderBy(g => g.Key)
            .ToList();

        var firstPlace = groups.Count > 0 ? groups[0].Select(r => r.PersonId).ToArray() : [];
        var secondPlace = groups.Count > 1 ? groups[1].Select(r => r.PersonId).ToArray() : [];
        var thirdPlace = groups.Count > 2 ? groups[2].Select(r => r.PersonId).ToArray() : [];

        game.Complete(firstPlace, secondPlace, thirdPlace);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

`GetSimracingResultsQuery.cs`:
```csharp
namespace TronderLeikan.Application.Games.Queries.GetSimracingResults;
public record GetSimracingResultsQuery(Guid GameId);
```

`GetSimracingResultsQueryHandler.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Games.Dtos;

namespace TronderLeikan.Application.Games.Queries.GetSimracingResults;

public sealed class GetSimracingResultsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetSimracingResultsQuery, SimracingResultDto[]>
{
    public async Task<Result<SimracingResultDto[]>> Handle(GetSimracingResultsQuery query, CancellationToken ct = default)
    {
        var results = await db.SimracingResults
            .Where(r => r.GameId == query.GameId)
            .OrderBy(r => r.RaceTimeMs)
            .Select(r => new SimracingResultDto(r.Id, r.PersonId, r.RaceTimeMs))
            .ToArrayAsync(ct);
        return Result<SimracingResultDto[]>.Ok(results);
    }
}
```

**Step 3: Kjør tester og commit**

```bash
dotnet test tests/TronderLeikan.Application.Tests/ --filter "FullyQualifiedName~SimracingHandlerTests"
git add src/TronderLeikan.Application/ tests/TronderLeikan.Application.Tests/
git commit -m "feat: implementer Simracing-handlers (RegisterResult, CompleteGame, GetResults)"
```

---

## Task 19: Domain Event Handler-stubs

**Files:**
- Create: `src/TronderLeikan.Application/Games/EventHandlers/GameCompletedEventHandler.cs`
- Create: `src/TronderLeikan.Application/Games/EventHandlers/SimracingResultRegisteredEventHandler.cs`

**Step 1: GameCompletedEventHandler.cs** (placeholder for fremtidig notifisering)

```csharp
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
```

**Step 2: SimracingResultRegisteredEventHandler.cs**

```csharp
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
```

**Step 3: Bygg og commit**

```bash
dotnet build
git add src/TronderLeikan.Application/
git commit -m "feat: legg til domenehendelse-handlers (placeholder)"
```

---

## Task 20: Final build og full testkjøring

**Step 1: Bygg hele solution**

```bash
dotnet build /Users/svedanie/crayon/TronderLeikan/TronderLeikan.slnx
```
Forventet: Build succeeded, 0 errors, ≤ 0 warnings.

**Step 2: Kjør alle tester**

```bash
dotnet test /Users/svedanie/crayon/TronderLeikan/TronderLeikan.slnx
```
Forventet: Alle tester PASS. Sjekk at Application.Tests, Domain.Tests og Infrastructure.Tests alle kjører.

**Step 3: Sjekkliste**

- [ ] `dotnet build` — 0 errors
- [ ] `dotnet test` — alle tester PASS
- [ ] Scrutor scanner handlers korrekt (ingen manuelle registreringer)
- [ ] Result-pattern brukes konsekvent — ingen exceptions kastet fra handlers
- [ ] Kommentarer er på norsk med æøå
- [ ] Alle filnavn følger konvensjonen (PascalCase, fil-scoped namespaces)

**Step 4: Commit om alt er i orden**

```bash
git add .
git commit -m "test: verifisert at alle tester passerer etter Application-lag implementasjon"
```

---

## Påminnelse: Domain-oppdateringer som trengs

Under implementasjon vil du trenge å legge til disse metodene i domenet:

**`src/TronderLeikan.Domain/Persons/Person.cs`:**
```csharp
public void Update(string firstName, string lastName)
{
    FirstName = firstName;
    LastName = lastName;
}
```

**`src/TronderLeikan.Domain/Games/Game.cs`:**
```csharp
public void UpdateName(string name) => Name = name;
```

Legg disse til **før** du implementer de tilhørende handlerne.
