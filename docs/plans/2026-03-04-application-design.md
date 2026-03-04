# Application Design — TrønderLeikan

**Dato:** 2026-03-04
**Status:** Godkjent

---

## Kontekst

Application-laget orkestrerer domenet mot infrastruktur via use cases (commands/queries). Laget er uavhengig av EF Core, RabbitMQ og ImageSharp — disse er abstrahere bak interfaces. Designet tar høyde for **minst 2 replicas** fra dag én.

---

## Valg og begrunnelser

| Valg | Beslutning | Begrunnelse |
|---|---|---|
| CQRS-bibliotek | Ingen MediatR — egne `ICommandHandler`/`IQueryHandler` | Ingen ekstra avhengighet, tilstrekkelig for denne skalaen |
| Validering | FluentValidation | Rikt regelspråk, lett å teste isolert |
| Feilhåndtering | Result-pattern (`Result`, `Result<T>`) | Ingen exceptions for forventede feil, tydeligere kontraktgrenser |
| DI-scanning | Scrutor (`.Scan()`) | Automatisk handler-registrering uten manuell boilerplate |
| Domain events | Hybrid: state + EventStore + Outbox | State for enkel querying, EventStore for audit/tidsreise, Outbox for pub/sub |
| Message broker | RabbitMQ via Aspire | Læringsverdi, støtter multi-replica, lett å kjøre lokalt |
| Bildeprosessering | `IImageProcessor` i Application, ImageSharp i Infrastructure | Application vet ikke om ImageSharp |
| Tid | `IDateTimeProvider` | Testbar, ingen spredte `DateTime.UtcNow`-kall |
| Autentisering | Utsettes (Zitadel-kandidat) | `ICurrentUser`-interface klargjøres, kobles til når auth er valgt |

---

## Kjerneabstraksjoner

### Result-pattern

```csharp
// Application/Common/Results/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool success, string? error) { IsSuccess = success; Error = error; }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) { Value = value; }
    private Result(string error) : base(false, error) { }

    public static Result<T> Ok(T value) => new(value);
    public new static Result<T> Fail(string error) => new(error);
}
```

### Handler-interfaces

```csharp
// Kommando med returverdi
public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct = default);
}

// Kommando uten returverdi (void)
public interface ICommandHandler<TCommand>
{
    Task<Result> Handle(TCommand command, CancellationToken ct = default);
}

// Query — alltid med returverdi
public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct = default);
}
```

---

## Interfaces definert i Application

```csharp
// Mot EF Core (eksisterer)
IAppDbContext

// Mot message broker — implementert i Infrastructure
IMessagePublisher
    Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default)

// Mot domain event dispatching — implementert i Infrastructure
IDomainEventDispatcher
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)

// Mot bildeprosessering — implementert i Infrastructure (ImageSharp)
IImageProcessor
    Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct)
    Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct)

// Klokke — implementert i Infrastructure
IDateTimeProvider
    DateTime UtcNow { get; }
    DateOnly Today { get; }

// Fremtidig auth — implementert i API-laget
ICurrentUser
    Guid UserId { get; }
    bool IsAuthenticated { get; }
```

---

## Hybrid EventStore + Outbox

Tre ting skjer i **én transaksjon** i `AppDbContext.SaveChangesAsync`:

```
1. State       → Games, Persons, Tournaments (EF Core som nå)
2. EventStore  → append-only audit log per aggregat
3. Outbox      → events som skal publiseres til RabbitMQ
```

### EventStoreEntry (Infrastructure-entitet)

```csharp
public sealed class EventStoreEntry
{
    public Guid Id { get; set; }
    public string StreamId { get; set; }    // f.eks. GameId
    public string StreamType { get; set; }  // "Game", "Person"
    public string EventType { get; set; }   // "GameCompletedEvent"
    public string Payload { get; set; }     // JSON
    public int Version { get; set; }        // per stream — optimistic concurrency
    public DateTime OccurredAt { get; set; }
}
```

Tidsreise: `SELECT * FROM EventStore WHERE StreamId = @id ORDER BY Version`

### OutboxMessage (Infrastructure-entitet)

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
```

### OutboxProcessor (IHostedService i Infrastructure)

- Poller `OutboxMessages` med `FOR UPDATE SKIP LOCKED` — kun én replica jobber med samme rad
- Publiserer til RabbitMQ via `IMessagePublisher`
- Markerer som `ProcessedAt`

### ProcessedEvent (idempotens)

```csharp
public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }      // RabbitMQ message ID
    public DateTime ProcessedAt { get; set; }
}
```

Consumers sjekker `ProcessedEvents`-tabellen før behandling — beskytter mot duplikat-levering.

### Broker-agnostisk publisering

Bytte broker = én linje i DI:
```csharp
services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();   // nå
services.AddSingleton<IMessagePublisher, KafkaPublisher>();      // fremtid
services.AddSingleton<IMessagePublisher, ServiceBusPublisher>(); // Azure
services.AddSingleton<IMessagePublisher, InMemoryPublisher>();   // tester
```

---

## Use cases

### Commands

| Handler | Input | Output |
|---|---|---|
| `CreateDepartmentCommand` | Name | `Result<Guid>` |
| `CreatePersonCommand` | FirstName, LastName, DepartmentId? | `Result<Guid>` |
| `UpdatePersonCommand` | PersonId, FirstName, LastName, DepartmentId? | `Result` |
| `DeletePersonCommand` | PersonId | `Result` |
| `UploadPersonImageCommand` | PersonId, Stream | `Result` |
| `DeletePersonImageCommand` | PersonId | `Result` |
| `CreateTournamentCommand` | Name, Slug | `Result<Guid>` |
| `UpdateTournamentPointRulesCommand` | TournamentId, alle 7 verdier | `Result` |
| `CreateGameCommand` | TournamentId, Name, GameType | `Result<Guid>` |
| `UpdateGameCommand` | GameId, Name, Description? | `Result` |
| `AddParticipantCommand` | GameId, PersonId | `Result` |
| `AddOrganizerCommand` | GameId, PersonId, WithParticipation | `Result` |
| `AddSpectatorCommand` | GameId, PersonId | `Result` |
| `CompleteGameCommand` | GameId, FirstPlace[], SecondPlace[], ThirdPlace[] | `Result` |
| `UploadGameBannerCommand` | GameId, Stream | `Result` |
| `RegisterSimracingResultCommand` | GameId, PersonId, RaceTimeMs | `Result<Guid>` |
| `CompleteSimracingGameCommand` | GameId | `Result` |

### Queries

| Handler | Input | Output |
|---|---|---|
| `GetDepartmentsQuery` | — | `Result<DepartmentDto[]>` |
| `GetPersonsQuery` | — | `Result<PersonSummaryDto[]>` |
| `GetPersonByIdQuery` | PersonId | `Result<PersonDetailDto>` |
| `GetTournamentsQuery` | — | `Result<TournamentSummaryDto[]>` |
| `GetTournamentBySlugQuery` | Slug | `Result<TournamentDetailDto>` |
| `GetScoreboardQuery` | TournamentId | `Result<ScoreboardEntryDto[]>` |
| `GetGameByIdQuery` | GameId | `Result<GameDetailDto>` |
| `GetSimracingResultsQuery` | GameId | `Result<SimracingResultDto[]>` |

### Domain event handlers (kjøres via RabbitMQ consumer)

| Handler | Event | Gjør |
|---|---|---|
| `GameCompletedEventHandler` | `GameCompletedEvent` | Placeholder — notifisering fremover |
| `SimracingResultRegisteredEventHandler` | `SimracingResultRegisteredEvent` | Placeholder |

---

## CompleteSimracingGameCommand — logikk

Beregner plasseringer på tvers av alle registrerte tider:

```
1. Hent alle SimracingResults for GameId
2. Sorter på RaceTimeMs ascending (lavest = 1. plass)
3. Håndter ties: like tider → deler plassering
4. Kall game.Complete(firstPlace[], secondPlace[], thirdPlace[])
5. SaveChangesAsync → state + EventStore + Outbox i én transaksjon
```

---

## GetScoreboardQuery — beregningslogikk

On-the-fly (maks 100 × 12 = trivielt):

```
For hvert IsDone-spill i turneringen:
  - Deltakere: +participation
  - 1./2./3. plass: +firstPlace/secondPlace/thirdPlace (additive)
  - Arrangør med deltakelse: +organizedWithParticipation + participation
  - Arrangør uten deltakelse: +organizedWithoutParticipation
  - Tilskuere: +spectator

Sorter synkende på totalpoeng.
Ties: samme rank. Neste person hopper over rank(er).
```

---

## Mappestruktur

```
src/TronderLeikan.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IAppDbContext.cs
│   │   ├── ICommandHandler.cs
│   │   ├── IQueryHandler.cs
│   │   ├── IMessagePublisher.cs
│   │   ├── IDomainEventDispatcher.cs
│   │   ├── IImageProcessor.cs
│   │   ├── IDateTimeProvider.cs
│   │   └── ICurrentUser.cs          (stub — kobles til auth senere)
│   ├── Results/
│   │   └── Result.cs
│   └── DependencyInjection.cs       (Scrutor-scanning)
│
├── Departments/Commands/CreateDepartment/
├── Persons/
│   ├── Commands/{CreatePerson,UpdatePerson,DeletePerson,UploadPersonImage,DeletePersonImage}/
│   └── Queries/{GetPersons,GetPersonById}/
├── Tournaments/
│   ├── Commands/{CreateTournament,UpdateTournamentPointRules}/
│   └── Queries/{GetTournaments,GetTournamentBySlug,GetScoreboard}/
├── Games/
│   ├── Commands/{CreateGame,UpdateGame,AddParticipant,AddOrganizer,AddSpectator,
│   │            CompleteGame,UploadGameBanner,RegisterSimracingResult,CompleteSimracingGame}/
│   ├── Queries/{GetGameById,GetSimracingResults}/
│   └── EventHandlers/{GameCompletedEventHandler,SimracingResultRegisteredEventHandler}/
└── Persistence/Images/              (PersonImage, GameBanner — eksisterer)
```

---

## DependencyInjection.cs (Application)

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    // Automatisk registrering via Scrutor
    services.Scan(scan => scan
        .FromAssemblyOf<IAppDbContext>()
        .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces().WithScopedLifetime()
        .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces().WithScopedLifetime()
        .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces().WithScopedLifetime());

    // FluentValidation — automatisk registrering av alle validators
    services.AddValidatorsFromAssemblyContaining<IAppDbContext>();

    return services;
}
```

---

## NuGet-pakker

| Prosjekt | Pakke |
|---|---|
| Application | `FluentValidation` |
| Application | `Scrutor` |
| Infrastructure | `RabbitMQ.Client` (eller `MassTransit.RabbitMQ`) |
| Infrastructure | `SixLabors.ImageSharp` |
| Infrastructure | `Aspire.RabbitMQ.Client` |
| AppHost | `Aspire.Hosting.RabbitMQ` |

---

## Fremtidig: Autentisering

```csharp
// Application/Common/Interfaces/ICurrentUser.cs
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
```

Zitadel (eller Entra ID / Auth0) kobles til i API-laget som implementerer `ICurrentUser` via `ClaimsPrincipal`. Ingen endringer i eksisterende handlers ved innføring.

---

## Nye tabeller (krever ny EF Core migration)

| Tabell | Beskrivelse |
|---|---|
| `EventStore` | Append-only audit log per aggregat |
| `OutboxMessages` | Events som venter på RabbitMQ-publisering |
| `ProcessedEvents` | Idempotens — allerede behandlede events |
