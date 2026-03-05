# ISender — egendefinert mediator med pipeline

**Goal:** Erstatt N `ICommandHandler`/`IQueryHandler` constructor-parametre i controllers med én `ISender`, og legg til automatisk validering og observabilitet via pipeline-behaviors.

**Architecture:** `ISender` løser opp handlers via `IServiceProvider.MakeGenericType` og kjører dem gjennom en chain av `IPipelineBehavior<TRequest, TResponse>`. Marker interfaces (`ICommand<T>`, `ICommand`, `IQuery<T>`) gir type-inferens. `ObservabilityBehavior` og `ValidationBehavior` registreres som åpne generics via Scrutor og plukkes opp automatisk for alle fremtidige commands/queries.

**Tech Stack:** .NET 10, Scrutor (allerede i bruk), FluentValidation (allerede i bruk), System.Diagnostics.ActivitySource, System.Diagnostics.Metrics

---

## Task 1: `IResult` — felles marker for `Result` og `Result<T>`

`ObservabilityBehavior` trenger å inspisere om resultatet er success/failure uten å kjenne den konkrete typen. `IResult` gir dette uten refleksjon.

**Files:**
- Modify: `src/TronderLeikan.Application/Common/Results/Result.cs`

**Step 1: Legg til `IResult`-interface og implementer det**

Åpne `src/TronderLeikan.Application/Common/Results/Result.cs`.

Legg til interfacet øverst i filen (over `Result`-klassen):

```csharp
public interface IResult
{
    bool IsSuccess { get; }
    Error? Error { get; }
}
```

Endre `Result`-klassen til å implementere det:

```csharp
public class Result : IResult
{
    // resten er uendret
```

Endre `Result<T>`-klassen (den arver allerede fra `Result`, som nå implementerer `IResult` — ingen endring nødvendig).

**Step 2: Bygg og verifiser**

```bash
dotnet build src/TronderLeikan.Application --no-incremental -v q
```

Forventet: `0 Error(s)`

**Step 3: Commit**

```bash
git add src/TronderLeikan.Application/Common/Results/Result.cs
git commit -m "feat(application): legg til IResult-interface på Result og Result<T>"
```

---

## Task 2: Marker interfaces — `ICommand<T>`, `ICommand`, `IQuery<T>`

Disse er tomme marker interfaces som commands og queries implementerer. De gir `ISender` type-inferens uten å endre `ICommandHandler`/`IQueryHandler`.

**Files:**
- Create: `src/TronderLeikan.Application/Common/Interfaces/ICommand.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/IQuery.cs`

**Step 1: Opprett `ICommand.cs`**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Marker for kommando med returverdi — gir type-inferens i ISender.Send<TResult>()
public interface ICommand<TResult> { }

// Marker for kommando uten returverdi — gir type-inferens i ISender.Send()
public interface ICommand { }
```

**Step 2: Opprett `IQuery.cs`**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Marker for query — gir type-inferens i ISender.Query<TResult>()
public interface IQuery<TResult> { }
```

**Step 3: Bygg**

```bash
dotnet build src/TronderLeikan.Application --no-incremental -v q
```

Forventet: `0 Error(s)`

**Step 4: Commit**

```bash
git add src/TronderLeikan.Application/Common/Interfaces/ICommand.cs \
        src/TronderLeikan.Application/Common/Interfaces/IQuery.cs
git commit -m "feat(application): legg til ICommand og IQuery marker interfaces"
```

---

## Task 3: Legg marker interfaces på alle commands og queries

23 filer — én linje endring per fil. Ingen logikk endres.

**Files:** (alle under `src/TronderLeikan.Application/`)

Marker interface-regler:
- Returnerer `Result<Guid>` → implementer `ICommand<Guid>`
- Returnerer `Result` (void) → implementer `ICommand`
- Query → implementer `IQuery<TResultType>`

**Step 1: Oppdater alle commands**

Endre hver command-record til å implementere riktig marker. Eksempel på mønsteret:

```csharp
// Før:
public record CreateGameCommand(Guid TournamentId, string Name, GameType GameType);

// Etter:
public record CreateGameCommand(Guid TournamentId, string Name, GameType GameType) : ICommand<Guid>;
```

Fullstendig liste med riktig marker:

| Fil | Marker |
|---|---|
| `Departments/Commands/CreateDepartment/CreateDepartmentCommand.cs` | `: ICommand<Guid>` |
| `Games/Commands/AddOrganizer/AddOrganizerCommand.cs` | `: ICommand` |
| `Games/Commands/AddParticipant/AddParticipantCommand.cs` | `: ICommand` |
| `Games/Commands/AddSpectator/AddSpectatorCommand.cs` | `: ICommand` |
| `Games/Commands/CompleteGame/CompleteGameCommand.cs` | `: ICommand` |
| `Games/Commands/CompleteSimracingGame/CompleteSimracingGameCommand.cs` | `: ICommand` |
| `Games/Commands/CreateGame/CreateGameCommand.cs` | `: ICommand<Guid>` |
| `Games/Commands/RegisterSimracingResult/RegisterSimracingResultCommand.cs` | `: ICommand<Guid>` |
| `Games/Commands/UpdateGame/UpdateGameCommand.cs` | `: ICommand` |
| `Games/Commands/UploadGameBanner/UploadGameBannerCommand.cs` | `: ICommand` |
| `Persons/Commands/CreatePerson/CreatePersonCommand.cs` | `: ICommand<Guid>` |
| `Persons/Commands/DeletePerson/DeletePersonCommand.cs` | `: ICommand` |
| `Persons/Commands/DeletePersonImage/DeletePersonImageCommand.cs` | `: ICommand` |
| `Persons/Commands/UpdatePerson/UpdatePersonCommand.cs` | `: ICommand` |
| `Persons/Commands/UploadPersonImage/UploadPersonImageCommand.cs` | `: ICommand` |
| `Tournaments/Commands/CreateTournament/CreateTournamentCommand.cs` | `: ICommand<Guid>` |
| `Tournaments/Commands/UpdateTournamentPointRules/UpdateTournamentPointRulesCommand.cs` | `: ICommand` |
| `Departments/Queries/GetDepartments/GetDepartmentsQuery.cs` | `: IQuery<DepartmentResponse[]>` |
| `Games/Queries/GetGameById/GetGameByIdQuery.cs` | `: IQuery<GameDetailResponse>` |
| `Games/Queries/GetSimracingResults/GetSimracingResultsQuery.cs` | `: IQuery<SimracingResultResponse[]>` |
| `Persons/Queries/GetPersonById/GetPersonByIdQuery.cs` | `: IQuery<PersonDetailResponse>` |
| `Persons/Queries/GetPersons/GetPersonsQuery.cs` | `: IQuery<PersonSummaryResponse[]>` |
| `Tournaments/Queries/GetScoreboard/GetScoreboardQuery.cs` | `: IQuery<ScoreboardEntryResponse[]>` |
| `Tournaments/Queries/GetTournamentBySlug/GetTournamentBySlugQuery.cs` | `: IQuery<TournamentDetailResponse>` |
| `Tournaments/Queries/GetTournaments/GetTournamentsQuery.cs` | `: IQuery<TournamentSummaryResponse[]>` |

Husk å legge til `using TronderLeikan.Application.Common.Interfaces;` i filer som ikke allerede har det. Sjekk om namespacet for response-typen er importert i query-filer (mange queries er i eget namespace og trenger `using`).

**Step 2: Bygg og verifiser**

```bash
dotnet build src/TronderLeikan.Application --no-incremental -v q
```

Forventet: `0 Error(s)`

**Step 3: Commit**

```bash
git add src/TronderLeikan.Application
git commit -m "feat(application): legg til ICommand/IQuery marker interfaces på alle commands og queries"
```

---

## Task 4: `IPipelineBehavior` og `ISender` interfaces

**Files:**
- Create: `src/TronderLeikan.Application/Common/Interfaces/IPipelineBehavior.cs`
- Create: `src/TronderLeikan.Application/Common/Interfaces/ISender.cs`

**Step 1: Opprett `IPipelineBehavior.cs`**

```csharp
namespace TronderLeikan.Application.Common.Interfaces;

// Pipeline-kontrakt — behaviors kjøres i rekkefølge rundt handleren
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
```

**Step 2: Opprett `ISender.cs`**

```csharp
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Interfaces;

// Mediator-interface — controllers injiserer kun denne
public interface ISender
{
    // Kommando med returverdi (f.eks. opprett-operasjoner som returnerer ny Id)
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);

    // Kommando uten returverdi (f.eks. oppdater, slett)
    Task<Result> Send(ICommand command, CancellationToken ct = default);

    // Query — alltid med returverdi
    Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
```

**Step 3: Bygg**

```bash
dotnet build src/TronderLeikan.Application --no-incremental -v q
```

Forventet: `0 Error(s)`

**Step 4: Commit**

```bash
git add src/TronderLeikan.Application/Common/Interfaces/IPipelineBehavior.cs \
        src/TronderLeikan.Application/Common/Interfaces/ISender.cs
git commit -m "feat(application): legg til IPipelineBehavior og ISender interfaces"
```

---

## Task 5: `ValidationBehavior`

Kjører FluentValidation-validators automatisk for alle requests. Returnerer `Error.Validation` ved feil — handlers trenger ikke validere manuelt etterpå.

**Files:**
- Create: `src/TronderLeikan.Application/Common/Behaviors/ValidationBehavior.cs`
- Test: `tests/TronderLeikan.Application.Tests/Common/Behaviors/ValidationBehaviorTests.cs`

**Step 1: Skriv failing test**

Opprett `tests/TronderLeikan.Application.Tests/Common/Behaviors/ValidationBehaviorTests.cs`:

```csharp
using FluentValidation;
using FluentValidation.Results;
using TronderLeikan.Application.Common.Behaviors;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Tests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    private record TestCommand(string Name);

    private sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator() =>
            RuleFor(c => c.Name).NotEmpty().WithMessage("Navn er påkrevd.");
    }

    [Fact]
    public async Task Handle_GyldigRequest_KallerNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>(
            [new TestValidator()]);
        var nextKalt = false;
        var expectedId = Guid.NewGuid();

        // Act
        var result = await behavior.Handle(
            new TestCommand("Gyldig navn"),
            () => { nextKalt = true; return Task.FromResult<Result<Guid>>(expectedId); },
            CancellationToken.None);

        // Assert
        nextKalt.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task Handle_UgyldigRequest_ReturnererValidationError()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>(
            [new TestValidator()]);

        // Act
        var result = await behavior.Handle(
            new TestCommand(""),
            () => throw new Exception("next skal ikke kalles"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Contain("Navn er påkrevd.");
    }

    [Fact]
    public async Task Handle_IngenValidators_KallerNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>([]);
        var nextKalt = false;

        // Act
        await behavior.Handle(
            new TestCommand(""),
            () => { nextKalt = true; return Task.FromResult<Result<Guid>>(Guid.NewGuid()); },
            CancellationToken.None);

        // Assert
        nextKalt.Should().BeTrue();
    }
}
```

**Step 2: Kjør failing test**

```bash
dotnet test tests/TronderLeikan.Application.Tests --filter "ValidationBehaviorTests" -v q
```

Forventet: FAIL — `ValidationBehavior` finnes ikke ennå.

**Step 3: Implementer `ValidationBehavior`**

Opprett `src/TronderLeikan.Application/Common/Behaviors/ValidationBehavior.cs`:

```csharp
using FluentValidation;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Behaviors;

// Kjører FluentValidation-validators automatisk — returnerer Error.Validation ved brudd
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var error = Error.Validation(
            code: "Validation.Failed",
            description: string.Join("; ", failures.Select(f => f.ErrorMessage)));

        // dynamic løser implicit operator (Error → Result / Result<T>) ved runtime
        return (TResponse)(dynamic)error;
    }
}
```

**Step 4: Kjør tester**

```bash
dotnet test tests/TronderLeikan.Application.Tests --filter "ValidationBehaviorTests" -v q
```

Forventet: PASS — 3 tester.

**Step 5: Commit**

```bash
git add src/TronderLeikan.Application/Common/Behaviors/ValidationBehavior.cs \
        tests/TronderLeikan.Application.Tests/Common/Behaviors/ValidationBehaviorTests.cs
git commit -m "feat(application): ValidationBehavior for automatisk FluentValidation i pipeline"
```

---

## Task 6: `ObservabilityBehavior`

Tracing (Activity-span) og metrics (counter + latency-histogram) kombinert i én behavior.

**Files:**
- Create: `src/TronderLeikan.Application/Common/Behaviors/ObservabilityBehavior.cs`
- Test: `tests/TronderLeikan.Application.Tests/Common/Behaviors/ObservabilityBehaviorTests.cs`

**Step 1: Skriv failing test**

Opprett `tests/TronderLeikan.Application.Tests/Common/Behaviors/ObservabilityBehaviorTests.cs`:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using TronderLeikan.Application.Common.Behaviors;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Tests.Common.Behaviors;

public sealed class ObservabilityBehaviorTests : IDisposable
{
    private readonly List<Activity> _recordedActivities = [];
    private readonly ActivityListener _listener;

    public ObservabilityBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "TronderLeikan.Sender",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private record TestQuery(string Term);

    [Fact]
    public async Task Handle_SuccessResult_StarterOgStopperSpanUtenFeil()
    {
        // Arrange
        var behavior = new ObservabilityBehavior<TestQuery, Result<string>>();

        // Act
        await behavior.Handle(
            new TestQuery("søk"),
            () => Task.FromResult<Result<string>>("treff"),
            CancellationToken.None);

        // Assert — span er registrert med riktig navn
        _recordedActivities.Should().ContainSingle(a => a.DisplayName == "TestQuery");
        _recordedActivities[0].Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Handle_FailureResult_SeterFeilstatus()
    {
        // Arrange
        var behavior = new ObservabilityBehavior<TestQuery, Result<string>>();
        var error = Error.NotFound("Test.NotFound", "Ikke funnet");

        // Act
        await behavior.Handle(
            new TestQuery("søk"),
            () => Task.FromResult<Result<string>>(error),
            CancellationToken.None);

        // Assert
        _recordedActivities.Should().ContainSingle();
        _recordedActivities[0].Status.Should().Be(ActivityStatusCode.Error);
        _recordedActivities[0].GetTagItem("sender.error").Should().Be("Test.NotFound");
    }

    public void Dispose() => _listener.Dispose();
}
```

**Step 2: Kjør failing test**

```bash
dotnet test tests/TronderLeikan.Application.Tests --filter "ObservabilityBehaviorTests" -v q
```

Forventet: FAIL — `ObservabilityBehavior` finnes ikke.

**Step 3: Implementer `ObservabilityBehavior`**

Opprett `src/TronderLeikan.Application/Common/Behaviors/ObservabilityBehavior.cs`:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Behaviors;

// Tracing og metrics for alle commands og queries — synlig i Aspire Dashboard
internal sealed class ObservabilityBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    // Delt ActivitySource og Meter — registreres i AddServiceDefaults
    internal static readonly ActivitySource ActivitySource = new("TronderLeikan.Sender");
    internal static readonly Meter Meter = new("TronderLeikan.Sender");

    // Antall dispatchede requests, tagget med type og resultat
    private static readonly Counter<long> RequestCounter =
        Meter.CreateCounter<long>("sender.requests.total", description: "Antall dispatchede commands og queries");

    // Latency-histogram — grunnlag for P95/P99 alerts
    private static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("sender.requests.duration", "ms", description: "Varighet per request-type");

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag("sender.request", name);

        var response = await next();
        sw.Stop();

        // Inspiser resultatet via IResult — begge Result-typer implementerer dette
        var isFailure = response is IResult { IsSuccess: false };
        var errorCode = isFailure ? ((IResult)response).Error?.Code : null;

        if (isFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorCode);
            activity?.SetTag("sender.error", errorCode);
        }

        var tags = new TagList
        {
            { "request.type",   name },
            { "request.result", isFailure ? "failure" : "success" }
        };
        if (errorCode is not null)
            tags.Add("request.error_code", errorCode);

        RequestCounter.Add(1, tags);
        RequestDuration.Record(sw.Elapsed.TotalMilliseconds, tags);

        return response;
    }
}
```

**Step 4: Kjør tester**

```bash
dotnet test tests/TronderLeikan.Application.Tests --filter "ObservabilityBehaviorTests" -v q
```

Forventet: PASS — 2 tester.

**Step 5: Commit**

```bash
git add src/TronderLeikan.Application/Common/Behaviors/ObservabilityBehavior.cs \
        tests/TronderLeikan.Application.Tests/Common/Behaviors/ObservabilityBehaviorTests.cs
git commit -m "feat(application): ObservabilityBehavior med tracing og metrics for alle requests"
```

---

## Task 7: `Sender` og startup-validering

`Sender` løser opp handlers via `IServiceProvider` og kjører dem gjennom pipeline. `HandlerRegistrationValidator` sjekker at alle handlers er registrert ved oppstart — feiler appen i CI/CD ved manglende registrering.

**Files:**
- Create: `src/TronderLeikan.Application/Common/Sender.cs`
- Create: `src/TronderLeikan.Application/Common/HandlerRegistrationValidator.cs`

**Step 1: Implementer `Sender`**

Opprett `src/TronderLeikan.Application/Common/Sender.cs`:

```csharp
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common;

internal sealed class Sender(IServiceProvider sp) : ISender
{
    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) =>
        Dispatch<Result<TResult>>(
            command,
            typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)),
            ct);

    public Task<Result> Send(ICommand command, CancellationToken ct = default) =>
        Dispatch<Result>(
            command,
            typeof(ICommandHandler<>).MakeGenericType(command.GetType()),
            ct);

    public Task<Result<TResult>> Query<TResult>(IQuery<TResult> query, CancellationToken ct = default) =>
        Dispatch<Result<TResult>>(
            query,
            typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult)),
            ct);

    private async Task<TResponse> Dispatch<TResponse>(object request, Type handlerType, CancellationToken ct)
    {
        var handler = sp.GetService(handlerType);

        // Graceful fallback — returnerer strukturert feil i stedet for ukontrollert exception
        if (handler is null)
        {
            var error = Error.Unexpected(
                "Sender.HandlerNotFound",
                $"Ingen handler registrert for '{request.GetType().Name}'.");
            return (TResponse)(dynamic)error;
        }

        // Hent behaviors for konkret request-type og response-type
        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = sp.GetServices(behaviorType).ToList();

        // Bygg pipeline — ytterste behavior kjøres først (ObservabilityBehavior → ValidationBehavior → handler)
        Func<Task<TResponse>> pipeline = () =>
            (Task<TResponse>)handlerType.GetMethod("Handle")!.Invoke(handler, [request, ct])!;

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var inner = pipeline;
            pipeline = () => (Task<TResponse>)behavior.GetType()
                .GetMethod("Handle")!
                .Invoke(behavior, [request, inner, ct])!;
        }

        return await pipeline();
    }
}
```

**Step 2: Implementer `HandlerRegistrationValidator`**

Opprett `src/TronderLeikan.Application/Common/HandlerRegistrationValidator.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Common;

// Sjekker at alle commands og queries har registrert handler ved oppstart
// Feiler appen i CI/CD ved manglende registrering — ingen overraskelser i produksjon
internal sealed class HandlerRegistrationValidator(IServiceProvider sp) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ValidateHandlers();
        return next;
    }

    private void ValidateHandlers()
    {
        var assembly = typeof(IAppDbContext).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();
                var args = iface.GetGenericArguments();

                Type? handlerType = def switch
                {
                    var d when d == typeof(ICommand<>) =>
                        typeof(ICommandHandler<,>).MakeGenericType(type, args[0]),
                    var d when d == typeof(ICommand) && args.Length == 0 =>
                        typeof(ICommandHandler<>).MakeGenericType(type),
                    var d when d == typeof(IQuery<>) =>
                        typeof(IQueryHandler<,>).MakeGenericType(type, args[0]),
                    _ => null
                };

                if (handlerType is null) continue;

                // Kaster InvalidOperationException ved oppstart hvis handler mangler
                if (sp.GetService(handlerType) is null)
                    throw new InvalidOperationException(
                        $"Mangler handler-registrering for '{type.Name}'. " +
                        $"Forventet: {handlerType.Name}");
            }

            // Sjekk ICommand uten generics separat
            if (type.GetInterfaces().Any(i => i == typeof(ICommand)))
            {
                var handlerType = typeof(ICommandHandler<>).MakeGenericType(type);
                if (sp.GetService(handlerType) is null)
                    throw new InvalidOperationException(
                        $"Mangler handler-registrering for '{type.Name}'. " +
                        $"Forventet: ICommandHandler<{type.Name}>");
            }
        }
    }
}
```

**Step 3: Bygg**

```bash
dotnet build src/TronderLeikan.Application --no-incremental -v q
```

Forventet: `0 Error(s)`. (Sender og validator brukes ikke ennå — registreres i Task 8.)

**Step 4: Commit**

```bash
git add src/TronderLeikan.Application/Common/Sender.cs \
        src/TronderLeikan.Application/Common/HandlerRegistrationValidator.cs
git commit -m "feat(application): Sender med pipeline-dispatch og HandlerRegistrationValidator"
```

---

## Task 8: DI-registrering og OTel-konfigurasjon

**Files:**
- Modify: `src/TronderLeikan.Application/Common/DependencyInjection.cs`
- Modify: `src/TronderLeikan.API/Program.cs`

**Step 1: Oppdater `DependencyInjection.cs`**

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Behaviors;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Automatisk registrering av alle handlers og pipeline-behaviors via Scrutor
        services.Scan(scan => scan
            .FromAssemblyOf<IAppDbContext>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithScopedLifetime());

        // Pipeline-behaviors — rekkefølge styres av registreringsrekkefølge
        // ObservabilityBehavior ytterst (starter span + måler tid for hele kjøringen)
        // ValidationBehavior innerst (validerer før handler kalles)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // ISender — én avhengighet for alle controllers
        services.AddScoped<ISender, Sender>();

        // Startup-validering — feiler appen hvis handler mangler
        services.AddTransient<IStartupFilter, HandlerRegistrationValidator>();

        // FluentValidation — automatisk registrering av alle validators
        services.AddValidatorsFromAssemblyContaining<IAppDbContext>();

        return services;
    }
}
```

**Step 2: Oppdater `Program.cs` — legg til OTel-sources**

I `src/TronderLeikan.API/Program.cs`, etter `builder.AddServiceDefaults()`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("TronderLeikan.Sender"))
    .WithMetrics(m => m.AddMeter("TronderLeikan.Sender"));
```

**Step 3: Bygg hele solution**

```bash
dotnet build -v q
```

Forventet: `0 Error(s)`

**Step 4: Commit**

```bash
git add src/TronderLeikan.Application/Common/DependencyInjection.cs \
        src/TronderLeikan.API/Program.cs
git commit -m "feat: registrer ISender, pipeline-behaviors og OTel-sources"
```

---

## Task 9: Oppdater controllers

Erstatt alle individuelle handler-parametre med `ISender`. Controllers blir vesentlig enklere.

**Files:**
- Modify: `src/TronderLeikan.API/Controllers/DepartmentsController.cs`
- Modify: `src/TronderLeikan.API/Controllers/PersonsController.cs`
- Modify: `src/TronderLeikan.API/Controllers/TournamentsController.cs`
- Modify: `src/TronderLeikan.API/Controllers/GamesController.cs`

**Step 1: Oppdater `DepartmentsController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Departments.Commands.CreateDepartment;
using TronderLeikan.Application.Departments.Queries.GetDepartments;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.API.Controllers;

// Kontroller for avdelingsressurser — GET og POST
public sealed class DepartmentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DepartmentResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetDepartmentsQuery(), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetAll), null, id),
            Problem);
    }
}
```

**Step 2: Oppdater `PersonsController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Commands.CreatePerson;
using TronderLeikan.Application.Persons.Commands.DeletePerson;
using TronderLeikan.Application.Persons.Commands.DeletePersonImage;
using TronderLeikan.Application.Persons.Commands.UpdatePerson;
using TronderLeikan.Application.Persons.Commands.UploadPersonImage;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.API.Controllers;

// Kontroller for personressurser — CRUD og bildebehandling
public sealed class PersonsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PersonSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetPersonsQuery(), ct)).Match(Ok, Problem);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetPersonByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreatePersonCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdatePersonCommand command, CancellationToken ct) =>
        (await sender.Send(command with { PersonId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        (await sender.Send(new DeletePersonCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Laster opp profilbilde for en person
    [HttpPut("{id:guid}/image")]
    public async Task<ActionResult> UploadImage(Guid id, IFormFile image, CancellationToken ct)
    {
        await using var ms = await ToMemoryStreamAsync(image, ct);
        var result = await sender.Send(new UploadPersonImageCommand(id, ms), ct);
        return result.Match<ActionResult>(() => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult> DeleteImage(Guid id, CancellationToken ct) =>
        (await sender.Send(new DeletePersonImageCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);
}
```

**Step 3: Oppdater `TournamentsController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Tournaments.Commands.CreateTournament;
using TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;
using TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
using TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
using TronderLeikan.Application.Tournaments.Queries.GetTournaments;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class TournamentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TournamentSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetTournamentsQuery(), ct)).Match(Ok, Problem);

    // Slug-basert oppslag — brukes av frontend for navigasjon
    [HttpGet("{slug}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetBySlug(string slug, CancellationToken ct) =>
        (await sender.Query(new GetTournamentBySlugQuery(slug), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTournamentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetBySlug), new { slug = command.Slug }, id),
            Problem);
    }

    [HttpPut("{id:guid}/point-rules")]
    public async Task<ActionResult> UpdatePointRules(
        Guid id, UpdateTournamentPointRulesCommand command, CancellationToken ct) =>
        (await sender.Send(command with { TournamentId = id }, ct))
            .Match<ActionResult>(() => NoContent(), Problem);

    [HttpGet("{id:guid}/scoreboard")]
    public async Task<ActionResult<ScoreboardEntryResponse[]>> GetScoreboard(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetScoreboardQuery(id), ct)).Match(Ok, Problem);
}
```

**Step 4: Oppdater `GamesController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Commands.AddOrganizer;
using TronderLeikan.Application.Games.Commands.AddParticipant;
using TronderLeikan.Application.Games.Commands.AddSpectator;
using TronderLeikan.Application.Games.Commands.CompleteGame;
using TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
using TronderLeikan.Application.Games.Commands.CreateGame;
using TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
using TronderLeikan.Application.Games.Commands.UpdateGame;
using TronderLeikan.Application.Games.Commands.UploadGameBanner;
using TronderLeikan.Application.Games.Queries.GetGameById;
using TronderLeikan.Application.Games.Queries.GetSimracingResults;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class GamesController(ISender sender) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GameDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetGameByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateGameCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateGameCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult> AddParticipant(Guid id, AddParticipantCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/organizers")]
    public async Task<ActionResult> AddOrganizer(Guid id, AddOrganizerCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/spectators")]
    public async Task<ActionResult> AddSpectator(Guid id, AddSpectatorCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CompleteGameCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Banner-opplasting
    [HttpPut("{id:guid}/banner")]
    public async Task<ActionResult> UploadBanner(Guid id, IFormFile banner, CancellationToken ct)
    {
        await using var ms = await ToMemoryStreamAsync(banner, ct);
        var result = await sender.Send(new UploadGameBannerCommand(id, ms), ct);
        return result.Match<ActionResult>(() => NoContent(), Problem);
    }

    [HttpGet("{id:guid}/simracing-results")]
    public async Task<ActionResult<SimracingResultResponse[]>> GetSimracingResults(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetSimracingResultsQuery(id), ct)).Match(Ok, Problem);

    [HttpPost("{id:guid}/simracing-results")]
    public async Task<ActionResult<Guid>> RegisterSimracingResult(
        Guid id, RegisterSimracingResultCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { GameId = id }, ct);
        return result.Match(
            resultId => CreatedAtAction(nameof(GetSimracingResults), new { id }, resultId),
            Problem);
    }

    [HttpPost("{id:guid}/simracing-results/complete")]
    public async Task<ActionResult> CompleteSimracing(Guid id, CancellationToken ct) =>
        (await sender.Send(new CompleteSimracingGameCommand(id), ct))
            .Match<ActionResult>(() => NoContent(), Problem);
}
```

**Step 5: Bygg og kjør alle tester**

```bash
dotnet build -v q
```

Forventet: `0 Error(s)`

```bash
dotnet test tests/TronderLeikan.Api.Tests -v q
```

Forventet: `Passed! — Failed: 0, Passed: 21`

**Step 6: Commit**

```bash
git add src/TronderLeikan.API/Controllers/
git commit -m "refactor(api): erstatt individuelle handler-parametre med ISender i alle controllers"
```

---

## Task 10: Kjør alle tester og push

**Step 1: Kjør hele test-suiten**

```bash
dotnet test -v q
```

Forventet: Alle tester passerer, `0 Failed`.

**Step 2: Push og oppdater PR**

```bash
git push
```

PR #1 oppdateres automatisk med alle nye commits.
