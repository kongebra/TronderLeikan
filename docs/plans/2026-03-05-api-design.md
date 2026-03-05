# API-design — TrønderLeikan

**Dato:** 2026-03-05
**Status:** Godkjent

---

## Kontekst

API-laget eksponerer Application-lagets handlers via HTTP. Det refaktorerer også eksisterende `Result`-pattern til å bruke en sterk `Error`-type med `ErrorType`-enum og implicit operators. Versjonering innføres fra dag én for å sikre stabile kontrakter ved fremtidige refaktoreringer.

---

## Valg og begrunnelser

| Valg | Beslutning | Begrunnelse |
|---|---|---|
| API-stil | Controllers | Klassisk struktur, kjent konvensjon |
| Feilhåndtering | `Match` + `Problem(Error)` overload i `ApiControllerBase` | RFC 9457 via `ProblemDetailsFactory`, fleksibel, kortform tilgjengelig |
| Result-refaktorering | `Error`-record + implicit operators | Ingen magic strings, handlers returnerer direkte verdi eller feil |
| Versjonering | URL-segment (`/api/v1/`) via `Asp.Versioning.AspNetCore` | Eksplisitt, klientvennlig, akseptansetester holder ved breaking changes |
| Akseptansetester | `WebApplicationFactory` + Testcontainers.PostgreSql | Ekte database, black-box mot HTTP-kontrakten — ingen Application-typer i testprosjektet |
| Problem Details type | `about:blank` (defaultverdi via `ProblemDetailsFactory`) | Ingen tilhørende dokumentasjons-URL ennå |

---

## Del 1: Result-refaktorering (Application-laget)

### ErrorType

```csharp
public enum ErrorType
{
    Validation,           // 400
    Unauthorized,         // 401
    Forbidden,            // 403
    NotFound,             // 404
    MethodNotAllowed,     // 405
    Conflict,             // 409
    Gone,                 // 410
    UnprocessableEntity,  // 422
    TooManyRequests,      // 429
    Unexpected,           // 500
    ServiceUnavailable    // 503
}
```

### Error-record

```csharp
// Application/Common/Errors/Error.cs
public record Error(string Code, ErrorType Type, string Description)
{
    public static Error NotFound(string code, string desc)       => new(code, ErrorType.NotFound, desc);
    public static Error Validation(string code, string desc)     => new(code, ErrorType.Validation, desc);
    public static Error Conflict(string code, string desc)       => new(code, ErrorType.Conflict, desc);
    public static Error Forbidden(string code, string desc)      => new(code, ErrorType.Forbidden, desc);
    public static Error Unauthorized(string code, string desc)   => new(code, ErrorType.Unauthorized, desc);
    public static Error Unexpected(string code, string desc)     => new(code, ErrorType.Unexpected, desc);
}
```

### Result med implicit operators og Match

```csharp
// Application/Common/Results/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error) { IsSuccess = isSuccess; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);

    // Kortform for void Result
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error!);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error) : base(false, error) { }

    // Handler returnerer T direkte — ingen Result.Success()-kall
    public static implicit operator Result<T>(T value) => new(value);

    // Handler returnerer Error direkte — ingen Result.Fail()-kall
    public static implicit operator Result<T>(Error error) => new(error);

    // Full form — fleksibel
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
```

### Domenespesifikke feil

```csharp
// Application/Common/Errors/PersonErrors.cs
public static class PersonErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Person.NotFound", "Personen finnes ikke.");
}

// Application/Common/Errors/TournamentErrors.cs
public static class TournamentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Tournament.NotFound", "Turneringen finnes ikke.");
    public static readonly Error SlugTaken =
        Error.Conflict("Tournament.SlugTaken", "Slug er allerede i bruk.");
}

// Application/Common/Errors/GameErrors.cs
public static class GameErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Game.NotFound", "Spillet finnes ikke.");
    public static readonly Error AlreadyCompleted =
        Error.Conflict("Game.AlreadyCompleted", "Spillet er allerede fullført.");
}

// Application/Common/Errors/DepartmentErrors.cs
public static class DepartmentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Department.NotFound", "Avdelingen finnes ikke.");
}
```

### Handler-eksempel etter refaktorering

```csharp
// Før: return Result<PersonDetailResponse>.Fail("Person med Id ... finnes ikke.");
// Etter:
if (person is null) return PersonErrors.NotFound;       // Error → Result<T> (implicit)
return new PersonDetailResponse(person.Id, ...);        // T → Result<T> (implicit)
```

---

## Del 2: ApiControllerBase

```csharp
// API/Common/ApiControllerBase.cs
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    // Overload — tar vår Error-type, bruker ProblemDetailsFactory (RFC 9457)
    protected ObjectResult Problem(Error error) =>
        Problem(
            detail: error.Description,
            title: error.Code,
            statusCode: error.Type.ToHttpStatus());
}

// API/Common/ErrorTypeExtensions.cs
public static class ErrorTypeExtensions
{
    public static int ToHttpStatus(this ErrorType type) => type switch
    {
        ErrorType.Validation          => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized        => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden           => StatusCodes.Status403Forbidden,
        ErrorType.NotFound            => StatusCodes.Status404NotFound,
        ErrorType.MethodNotAllowed    => StatusCodes.Status405MethodNotAllowed,
        ErrorType.Conflict            => StatusCodes.Status409Conflict,
        ErrorType.Gone                => StatusCodes.Status410Gone,
        ErrorType.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
        ErrorType.TooManyRequests     => StatusCodes.Status429TooManyRequests,
        ErrorType.ServiceUnavailable  => StatusCodes.Status503ServiceUnavailable,
        _                             => StatusCodes.Status500InternalServerError
    };
}
```

### Controller-bruksmønster

```csharp
// Kortform — method groups (GET, de fleste queries)
return result.Match(Ok, Problem);

// Med lokasjon (POST som returnerer id)
return result.Match(
    id => CreatedAtAction(nameof(GetById), new { id }, id),
    Problem);

// NoContent (DELETE, PUT uten returverdi)
return result.Match(_ => NoContent(), Problem);

// Bildeopplasting
return result.Match(_ => NoContent(), Problem);
```

---

## Del 3: RFC 9457 Problem Details

`Problem()` fra `ControllerBase` bruker injisert `ProblemDetailsFactory` som setter `type`-feltet automatisk (defaulter til `about:blank`). Respons-format:

```json
{
  "type": "about:blank",
  "title": "Person.NotFound",
  "status": 404,
  "detail": "Personen finnes ikke."
}
```

---

## Del 4: URL-skjema (`/api/v1/`)

```
GET    /api/v1/departments
POST   /api/v1/departments

GET    /api/v1/persons
GET    /api/v1/persons/{id}
POST   /api/v1/persons
PUT    /api/v1/persons/{id}
DELETE /api/v1/persons/{id}
PUT    /api/v1/persons/{id}/image       ← multipart/form-data
DELETE /api/v1/persons/{id}/image

GET    /api/v1/tournaments
GET    /api/v1/tournaments/{slug}       ← slug for lesbar frontend-URL
POST   /api/v1/tournaments
PUT    /api/v1/tournaments/{id}/point-rules
GET    /api/v1/tournaments/{id}/scoreboard

GET    /api/v1/games/{id}
POST   /api/v1/games
PUT    /api/v1/games/{id}
POST   /api/v1/games/{id}/participants
POST   /api/v1/games/{id}/organizers
POST   /api/v1/games/{id}/spectators
POST   /api/v1/games/{id}/complete
PUT    /api/v1/games/{id}/banner        ← multipart/form-data
GET    /api/v1/games/{id}/simracing-results
POST   /api/v1/games/{id}/simracing-results
POST   /api/v1/games/{id}/simracing-results/complete
```

---

## Del 5: Mappestruktur

```
src/TronderLeikan.Application/
└── Common/
    └── Errors/
        ├── Error.cs
        ├── ErrorType.cs
        ├── DepartmentErrors.cs
        ├── PersonErrors.cs
        ├── TournamentErrors.cs
        └── GameErrors.cs

src/TronderLeikan.API/
├── Common/
│   ├── ApiControllerBase.cs
│   └── ErrorTypeExtensions.cs
├── Controllers/
│   ├── DepartmentsController.cs
│   ├── PersonsController.cs
│   ├── TournamentsController.cs
│   └── GamesController.cs
└── Program.cs

tests/TronderLeikan.Api.Tests/
├── TronderLeikanApiFactory.cs          (WebApplicationFactory + Testcontainers)
├── ApiTestCollection.cs               (xUnit ICollectionFixture)
├── Departments/
│   └── DepartmentsApiTests.cs
├── Persons/
│   └── PersonsApiTests.cs
├── Tournaments/
│   └── TournamentsApiTests.cs
└── Games/
    └── GamesApiTests.cs
```

---

## Del 6: NuGet-pakker

| Prosjekt | Pakke |
|---|---|
| Application | ingen nye |
| API | `Asp.Versioning.AspNetCore` |
| API.Tests | `Microsoft.AspNetCore.Mvc.Testing` |
| API.Tests | `Testcontainers.PostgreSql` |
| API.Tests | `FluentAssertions` |
| API.Tests | `Microsoft.EntityFrameworkCore.Design` (for MigrateAsync) |

---

## Del 7: Black-box akseptansetester

Testprosjektet refererer **kun** til `TronderLeikan.API`. Ingen `using` til Application- eller Domain-typer. Request-bodies sendes som anonyme objekter, responses parses som `JsonElement`.

```csharp
// TronderLeikanApiFactory.cs
public class TronderLeikanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}

// ApiTestCollection.cs
[CollectionDefinition(nameof(ApiTestCollection))]
public class ApiTestCollection : ICollectionFixture<TronderLeikanApiFactory> { }
```

Eksempel på tester:

```csharp
[Collection(nameof(ApiTestCollection))]
public class PersonsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_persons_returnerer_201_med_id()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Ola",
            lastName = "Nordmann"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_person_som_ikke_finnes_returnerer_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/v1/persons/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Person.NotFound");
        body.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task Opprett_og_hent_person_happy_path()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Kari",
            lastName = "Nordmann",
            departmentId = (Guid?)null
        });
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/persons/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Kari");
        body.GetProperty("lastName").GetString().Should().Be("Nordmann");
    }
}
```

---

## Del 8: API-versjonering

Versjonering via URL-segment fra dag én. Når v2 introduseres, holder eksisterende v1-tester og validerer at kontrakter ikke er brutt.

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
```
