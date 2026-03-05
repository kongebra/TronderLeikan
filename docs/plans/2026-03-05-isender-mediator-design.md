# Design: ISender — egendefinert mediator med pipeline

**Dato:** 2026-03-05
**Status:** Godkjent

## Bakgrunn

Controllere som `GamesController` har 11 constructor-parametre (`ICommandHandler<>` og `IQueryHandler<>`). Problemet vokser for hver ny operasjon. Samtidig ønsker vi cross-cutting concerns (validering, observabilitet) som kjøres automatisk for alle commands og queries uten å duplisere kode i handlers.

## Mål

1. Redusere controller constructor-parametre til **én**: `ISender`.
2. Validering (FluentValidation) kjøres automatisk via pipeline — handlers trenger ikke validere manuelt.
3. Tracing og metrics tilgjengelig for alle commands/queries uten endringer i handlers.
4. Ingen nye eksterne avhengigheter — Scrutor er allerede i bruk.

## Arkitektur

```
ISender (interface)
  └─ Sender (implementasjon — løser opp handlers via IServiceProvider + MakeGenericType)
       └─ Pipeline chain (Scrutor-scannede IPipelineBehavior<TRequest, TResponse>)
            ├─ ObservabilityBehavior  ← tracing + metrics
            └─ ValidationBehavior    ← FluentValidation
                 └─ ICommandHandler / IQueryHandler (selve handleren)
```

## Komponenter

### 1. Marker interfaces (`Application.Common.Interfaces`)

Tre nye interfaces som commands og queries implementerer. Gir type-inferens på kall-siden.

```csharp
public interface ICommand<TResult> { }
public interface ICommand { }
public interface IQuery<TResult> { }
```

Alle eksisterende command/query records får én ny linje:

```csharp
public sealed record CreateGameCommand(...) : ICommand<Guid>;
public sealed record UpdateGameCommand(...) : ICommand;
public sealed record GetGameByIdQuery(...) : IQuery<GameDetailResponse>;
```

### 2. `IResult` marker interface (`Application.Common.Results`)

`Result` og `Result<T>` implementerer `IResult` — brukes av pipeline behaviors for å inspisere resultatet uten å kjenne den konkrete typen.

```csharp
public interface IResult { bool IsSuccess { get; }  Error? Error { get; } }
```

### 3. `ISender` (`Application.Common.Interfaces`)

```csharp
public interface ISender
{
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<Result>          Send(ICommand command, CancellationToken ct = default);
    Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
```

### 4. `Sender` (`Application.Common`)

Løser opp riktig `ICommandHandler` / `IQueryHandler` fra `IServiceProvider` via `MakeGenericType`. Bruker `GetService` (ikke `GetRequiredService`) og returnerer `Error.Unexpected` ved manglende registrering — aldri ukontrollert exception.

Startup-validering (`IStartupFilter`) sjekker at alle kjente command/query-typer har registrert handler ved oppstart — feiler appen før første request, synlig i CI/CD.

### 5. `IPipelineBehavior<TRequest, TResponse>` (`Application.Common`)

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
```

`Sender` kjeder alle registrerte behaviors i rekkefølge. Nye behaviors plukkes automatisk opp av Scrutor-scannet — ingen endringer i `DependencyInjection.cs` nødvendig.

### 6. `ObservabilityBehavior` (`Application.Common.Behaviors`)

Kombinerer tracing (Activity-span) og metrics (counter + histogram) i én behavior.

- **Tracing**: `ActivitySource("TronderLeikan.Sender")` — child-span synlig i Aspire Dashboard.
- **Metrics**: `sender.requests.total` (Counter) og `sender.requests.duration` (Histogram i ms).
- **Tags**: `request.type`, `request.result` (success/failure), `request.error_code` (ved feil).

Krever to linjer i `AddServiceDefaults`-konfigurasjonen:
```csharp
.WithTracing(t => t.AddSource("TronderLeikan.Sender"))
.WithMetrics(m => m.AddMeter("TronderLeikan.Sender"))
```

### 7. `ValidationBehavior` (`Application.Common.Behaviors`)

Kjører alle `IValidator<TRequest>` registrert via FluentValidation. Returnerer `Error.Validation` ved feil via `(TResponse)(dynamic)error` — triggers implicit operator mot riktig Result-type. Hvis ingen validators er registrert hoppes laget over uten overhead.

### 8. Registrering (`Application.Common.DependencyInjection`)

```csharp
services.Scan(scan => scan
    // ...eksisterende handler-scan...
    .AddClasses(c => c.AssignableTo(typeof(IPipelineBehavior<,>)))
        .AsImplementedInterfaces().WithScopedLifetime());

services.AddScoped<ISender, Sender>();
```

### 9. Controller-oppdatering

`GamesController` (11 → 1 parameter), `PersonsController` (7 → 1), `TournamentsController` (5 → 1), `DepartmentsController` (2 → 1). Using-directives for individuelle handler-namespaces fjernes.

## Trade-offs

| Fordel | Ulempe |
|---|---|
| Controllere har én avhengighet | Runtime dispatch (handler-feil oppdages ved request, ikke compile-time) |
| Pipeline-behaviors kjøres automatisk | ~10 linjer `MakeGenericType`-kode i `Sender` vi eier selv |
| Tracing + metrics gratis for alle fremtidige commands | `(dynamic)` i `ValidationBehavior` (kun infrastrukturkode) |
| Scrutor-scan registrerer nye behaviors automatisk | Startup-validering er ekstra kode, men nødvendig |

## Scope

| Hva | Antall filer | Type |
|---|---|---|
| Nye interfaces, `ISender`, `Sender`, behaviors, startup-validering | ~8 nye filer | Ny infrastruktur |
| `DependencyInjection.cs` + `AppHost`-konfig | 2 | Endring |
| Commands/queries (marker interface — én linje per fil) | ~23 | Endring |
| `Result` / `Result<T>` (implementer `IResult`) | 2 | Endring |
| Controllers (constructor + usings) | 4 | Endring |
