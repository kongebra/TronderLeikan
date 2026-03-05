# API-lag Implementasjonsplan — TrønderLeikan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Mål:** Eksponer alle Application-handlers via versjonerte HTTP-endepunkter med RFC 9457-feilhåndtering og black-box akseptansetester.

**Arkitektur:** `Error`/`ErrorType`-pattern med implicit operators erstatter magic strings i Result-laget. `ApiControllerBase` med `Problem(Error)`-overload og `Match`-mønster i controllers. Black-box tester via `WebApplicationFactory` + Testcontainers mot ekte PostgreSQL — ingen Application-typer i testprosjektet.

**Tech Stack:** ASP.NET Core 10 Controllers, `Asp.Versioning.AspNetCore`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `FluentAssertions`, `System.Text.Json`

---

## Task 1: Error + ErrorType i Application-laget

**Files:**
- Create: `src/TronderLeikan.Application/Common/Errors/ErrorType.cs`
- Create: `src/TronderLeikan.Application/Common/Errors/Error.cs`

**Step 1: Opprett ErrorType.cs**

```csharp
// src/TronderLeikan.Application/Common/Errors/ErrorType.cs
namespace TronderLeikan.Application.Common.Errors;

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

**Step 2: Opprett Error.cs**

```csharp
// src/TronderLeikan.Application/Common/Errors/Error.cs
namespace TronderLeikan.Application.Common.Errors;

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

**Step 3: Bygg for å bekrefte at filene kompilerer**

```bash
dotnet build src/TronderLeikan.Application/TronderLeikan.Application.csproj --no-restore -v quiet
```

Forventet: `Build succeeded. 0 Error(s)` (Result.cs bruker fortsatt string — ingen konflikt ennå)

**Step 4: Commit**

```bash
git add src/TronderLeikan.Application/Common/Errors/
git commit -m "feat(application): legg til Error-record og ErrorType-enum"
```

---

## Task 2: Refaktorer Result.cs til å bruke Error

**Files:**
- Modify: `src/TronderLeikan.Application/Common/Results/Result.cs`

**Step 1: Erstatt innholdet i Result.cs**

```csharp
// src/TronderLeikan.Application/Common/Results/Result.cs
using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    // Implicit konvertering: Error → Result (void)
    public static implicit operator Result(Error error) => Failure(error);

    // Match for void-Result
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

    // Implicit konvertering: T → Result<T>
    public static implicit operator Result<T>(T value) => new(value);

    // Implicit konvertering: Error → Result<T>
    public static implicit operator Result<T>(Error error) => new(error);

    // Match for Result<T>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
```

**Step 2: Forsøk å bygge — forventer kompileringsfeil**

```bash
dotnet build src/TronderLeikan.Application/TronderLeikan.Application.csproj --no-restore -v quiet 2>&1 | grep "error CS"
```

Forventet: Feil i handlers som bruker `Result.Ok()`, `Result.Fail(string)`, `Result<T>.Ok(value)`, `Result<T>.Fail(string)`. Dette er forventet — fixes i Task 3.

**Step 3: Commit den nye Result.cs (kompilerer ikke ennå)**

```bash
git add src/TronderLeikan.Application/Common/Results/Result.cs
git commit -m "refactor(application): Result bruker Error-type med implicit operators og Match"
```

---

## Task 3: Domenespesifikke feil og oppdatering av alle handlers

**Files:**
- Create: `src/TronderLeikan.Application/Common/Errors/DepartmentErrors.cs`
- Create: `src/TronderLeikan.Application/Common/Errors/PersonErrors.cs`
- Create: `src/TronderLeikan.Application/Common/Errors/TournamentErrors.cs`
- Create: `src/TronderLeikan.Application/Common/Errors/GameErrors.cs`
- Modify: alle 18 handler-filer (se liste under)

**Step 1: Opprett domenespesifikke feil**

```csharp
// src/TronderLeikan.Application/Common/Errors/DepartmentErrors.cs
namespace TronderLeikan.Application.Common.Errors;
public static class DepartmentErrors
{
    public static readonly Error NotFound   = Error.NotFound("Department.NotFound", "Avdelingen finnes ikke.");
    public static readonly Error NameEmpty  = Error.Validation("Department.NameEmpty", "Navn kan ikke være tomt.");
}
```

```csharp
// src/TronderLeikan.Application/Common/Errors/PersonErrors.cs
namespace TronderLeikan.Application.Common.Errors;
public static class PersonErrors
{
    public static readonly Error NotFound = Error.NotFound("Person.NotFound", "Personen finnes ikke.");
}
```

```csharp
// src/TronderLeikan.Application/Common/Errors/TournamentErrors.cs
namespace TronderLeikan.Application.Common.Errors;
public static class TournamentErrors
{
    public static readonly Error NotFound  = Error.NotFound("Tournament.NotFound", "Turneringen finnes ikke.");
    public static readonly Error SlugTaken = Error.Conflict("Tournament.SlugTaken", "Slug er allerede i bruk.");
}
```

```csharp
// src/TronderLeikan.Application/Common/Errors/GameErrors.cs
namespace TronderLeikan.Application.Common.Errors;
public static class GameErrors
{
    public static readonly Error NotFound          = Error.NotFound("Game.NotFound", "Spillet finnes ikke.");
    public static readonly Error AlreadyCompleted  = Error.Conflict("Game.AlreadyCompleted", "Spillet er allerede fullført.");
    public static readonly Error NoSimracingResults = Error.Validation("Game.NoSimracingResults", "Ingen racetider registrert for dette spillet.");
}
```

**Step 2: Oppdater CreateDepartmentCommandHandler**

```csharp
// src/TronderLeikan.Application/Departments/Commands/CreateDepartment/CreateDepartmentCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
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
            return DepartmentErrors.NameEmpty;

        var department = Department.Create(command.Name);
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        return department.Id;
    }
}
```

**Step 3: Oppdater CreatePersonCommandHandler**

```csharp
// src/TronderLeikan.Application/Persons/Commands/CreatePerson/CreatePersonCommandHandler.cs
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
        return person.Id;
    }
}
```

**Step 4: Oppdater DeletePersonCommandHandler**

```csharp
// src/TronderLeikan.Application/Persons/Commands/DeletePerson/DeletePersonCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePerson;

public sealed class DeletePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonCommand>
{
    public async Task<Result> Handle(DeletePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;
        db.Persons.Remove(person);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Step 5: Oppdater UpdatePersonCommandHandler**

```csharp
// src/TronderLeikan.Application/Persons/Commands/UpdatePerson/UpdatePersonCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.UpdatePerson;

public sealed class UpdatePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdatePersonCommand>
{
    public async Task<Result> Handle(UpdatePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;
        person.Update(command.FirstName, command.LastName);
        person.UpdateDepartment(command.DepartmentId);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Step 6: Oppdater UploadPersonImageCommandHandler**

```csharp
// src/TronderLeikan.Application/Persons/Commands/UploadPersonImage/UploadPersonImageCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
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
        if (person is null) return PersonErrors.NotFound;

        var processedBytes = await imageProcessor.ProcessPersonImageAsync(command.ImageStream, ct);

        var existing = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (existing is not null)
            existing.ImageData = processedBytes;
        else
            db.PersonImages.Add(new PersonImage
            {
                PersonId = command.PersonId,
                ImageData = processedBytes,
                ContentType = "image/webp"
            });

        person.SetProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Step 7: Oppdater DeletePersonImageCommandHandler**

```csharp
// src/TronderLeikan.Application/Persons/Commands/DeletePersonImage/DeletePersonImageCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePersonImage;

public sealed class DeletePersonImageCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonImageCommand>
{
    public async Task<Result> Handle(DeletePersonImageCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;

        var image = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (image is not null) db.PersonImages.Remove(image);

        person.RemoveProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Step 8: Oppdater GetPersonByIdQueryHandler**

```csharp
// src/TronderLeikan.Application/Persons/Queries/GetPersonById/GetPersonByIdQueryHandler.cs
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonById;

public sealed class GetPersonByIdQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> Handle(GetPersonByIdQuery query, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([query.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;
        return new PersonDetailResponse(person.Id, person.FirstName, person.LastName, person.DepartmentId, person.HasProfileImage);
    }
}
```

**Step 9: Oppdater CreateTournamentCommandHandler**

```csharp
// src/TronderLeikan.Application/Tournaments/Commands/CreateTournament/CreateTournamentCommandHandler.cs
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
        return tournament.Id;
    }
}
```

**Step 10: Oppdater UpdateTournamentPointRulesCommandHandler**

```csharp
// src/TronderLeikan.Application/Tournaments/Commands/UpdateTournamentPointRules/UpdateTournamentPointRulesCommandHandler.cs
using TronderLeikan.Application.Common.Errors;
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
        if (tournament is null) return TournamentErrors.NotFound;

        tournament.UpdatePointRules(TournamentPointRules.Custom(
            command.Participation, command.FirstPlace, command.SecondPlace, command.ThirdPlace,
            command.OrganizedWithParticipation, command.OrganizedWithoutParticipation, command.Spectator));

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Step 11: Oppdater GetTournamentBySlugQueryHandler**

```csharp
// src/TronderLeikan.Application/Tournaments/Queries/GetTournamentBySlug/GetTournamentBySlugQueryHandler.cs
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;

public sealed class GetTournamentBySlugQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentBySlugQuery, TournamentDetailResponse>
{
    public async Task<Result<TournamentDetailResponse>> Handle(GetTournamentBySlugQuery query, CancellationToken ct = default)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(t => t.Slug == query.Slug, ct);
        if (t is null) return TournamentErrors.NotFound;

        var rules = new TournamentPointRulesResponse(
            t.PointRules.Participation, t.PointRules.FirstPlace, t.PointRules.SecondPlace,
            t.PointRules.ThirdPlace, t.PointRules.OrganizedWithParticipation,
            t.PointRules.OrganizedWithoutParticipation, t.PointRules.Spectator);
        return new TournamentDetailResponse(t.Id, t.Name, t.Slug, rules);
    }
}
```

**Step 12: Oppdater GetScoreboardQueryHandler**

Kun endre `Result<ScoreboardEntryResponse[]>.Fail(...)` → `TournamentErrors.NotFound` og fjern `using`-import for Results (beholder det via namespace):

```csharp
// Endre disse linjene i GetScoreboardQueryHandler.cs:
// Fra: if (tournament is null) return Result<ScoreboardEntryResponse[]>.Fail(...);
// Til:
if (tournament is null) return TournamentErrors.NotFound;
// Fra: return Result<ScoreboardEntryResponse[]>.Ok([.. entries]);
// Til:
return entries.ToArray();
```

Legg til `using TronderLeikan.Application.Common.Errors;` øverst.

**Step 13: Oppdater alle Game-handlers**

```csharp
// CreateGameCommandHandler.cs — endre returlinje
// Fra: return Result<Guid>.Ok(game.Id);
// Til:
return game.Id;
```

```csharp
// UpdateGameCommandHandler.cs
// Fra: if (game is null) return Result.Fail(...); ... return Result.Ok();
// Til:
if (game is null) return GameErrors.NotFound;
// ...
return Result.Success();
// Legg til: using TronderLeikan.Application.Common.Errors;
```

```csharp
// AddParticipantCommandHandler.cs, AddOrganizerCommandHandler.cs, AddSpectatorCommandHandler.cs
// Fra: if (game is null) return Result.Fail(...); ... return Result.Ok();
// Til:
if (game is null) return GameErrors.NotFound;
// ...
return Result.Success();
```

```csharp
// CompleteGameCommandHandler.cs
// Fra: if (game is null) return Result.Fail(...);
//      if (game.IsDone) return Result.Fail("...");
//      ... return Result.Ok();
// Til:
if (game is null) return GameErrors.NotFound;
if (game.IsDone) return GameErrors.AlreadyCompleted;
// ...
return Result.Success();
```

```csharp
// CompleteSimracingGameCommandHandler.cs
// Fra: if (game is null) return Result.Fail(...);
//      if (game.IsDone) return Result.Fail("...");
//      if (results.Count == 0) return Result.Fail("...");
//      ... return Result.Ok();
// Til:
if (game is null) return GameErrors.NotFound;
if (game.IsDone) return GameErrors.AlreadyCompleted;
if (results.Count == 0) return GameErrors.NoSimracingResults;
// ...
return Result.Success();
```

```csharp
// RegisterSimracingResultCommandHandler.cs
// Fra: if (game is null) return Result<Guid>.Fail(...);
//      if (game.IsDone) return Result<Guid>.Fail(...);
//      ... return Result<Guid>.Ok(result.Id);
// Til:
if (game is null) return GameErrors.NotFound;
if (game.IsDone) return GameErrors.AlreadyCompleted;
// ...
return result.Id;
```

```csharp
// UploadGameBannerCommandHandler.cs
// Fra: if (game is null) return Result.Fail(...); ... return Result.Ok();
// Til:
if (game is null) return GameErrors.NotFound;
// ...
return Result.Success();
```

```csharp
// GetGameByIdQueryHandler.cs
// Fra: if (game is null) return Result<GameDetailResponse>.Fail(...);
//      return Result<GameDetailResponse>.Ok(new GameDetailResponse(...));
// Til:
if (game is null) return GameErrors.NotFound;
return new GameDetailResponse(...);  // samme konstruktørargumenter
```

**Step 14: Oppdater Application.Tests som sjekker .Error (string)**

Søk etter tester som bruker `result.Error` som string:

```bash
grep -r "result\.Error" tests/TronderLeikan.Application.Tests/ --include="*.cs"
```

Hvis noen tester sjekker `result.Error == "..."` må de endres til:
```csharp
result.Error!.Code.Should().Be("Person.NotFound");
// eller
result.Error!.Type.Should().Be(ErrorType.NotFound);
```

**Step 15: Bygg og kjør alle tester**

```bash
dotnet build --no-restore -v quiet 2>&1 | tail -5
dotnet test tests/TronderLeikan.Application.Tests/ --no-build 2>&1 | tail -5
```

Forventet: `Build succeeded. 0 Error(s)` og `Passed! Failed: 0, Passed: 24`

**Step 16: Commit**

```bash
git add -A
git commit -m "refactor(application): erstatt magic strings med Error-typer i alle handlers og XxxErrors-klasser"
```

---

## Task 4: API-prosjekt setup — pakker, versjonering, Program.cs

**Files:**
- Modify: `src/TronderLeikan.API/TronderLeikan.API.csproj`
- Modify: `src/TronderLeikan.API/Program.cs`

**Step 1: Legg til versjonering-pakke**

```bash
cd src/TronderLeikan.API
dotnet add package Asp.Versioning.AspNetCore
cd ../..
```

**Step 2: Oppdater Program.cs**

```csharp
// src/TronderLeikan.API/Program.cs
using Asp.Versioning;
using TronderLeikan.Application.Common;
using TronderLeikan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

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

var connectionString = builder.Configuration.GetConnectionString("tronderleikan")
    ?? throw new InvalidOperationException("Connection string 'tronderleikan' ikke konfigurert.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseStatusCodePages();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();

// Gjør Program tilgjengelig for WebApplicationFactory i testprosjektet
public partial class Program { }
```

**Step 3: Bygg**

```bash
dotnet build src/TronderLeikan.API/TronderLeikan.API.csproj --no-restore -v quiet 2>&1 | tail -5
```

Forventet: `Build succeeded. 0 Error(s)`

**Step 4: Commit**

```bash
git add src/TronderLeikan.API/
git commit -m "feat(api): legg til Controllers, ApiVersioning og ProblemDetails i Program.cs"
```

---

## Task 5: ApiControllerBase og ErrorTypeExtensions

**Files:**
- Create: `src/TronderLeikan.API/Common/ErrorTypeExtensions.cs`
- Create: `src/TronderLeikan.API/Common/ApiControllerBase.cs`

**Step 1: Opprett ErrorTypeExtensions.cs**

```csharp
// src/TronderLeikan.API/Common/ErrorTypeExtensions.cs
using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.API.Common;

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

**Step 2: Opprett ApiControllerBase.cs**

```csharp
// src/TronderLeikan.API/Common/ApiControllerBase.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.API.Common;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    // Overload som tar vår Error-type — bruker ProblemDetailsFactory (RFC 9457)
    protected ObjectResult Problem(Error error) =>
        Problem(
            detail: error.Description,
            title: error.Code,
            statusCode: error.Type.ToHttpStatus());
}
```

**Step 3: Bygg**

```bash
dotnet build src/TronderLeikan.API/TronderLeikan.API.csproj --no-restore -v quiet 2>&1 | tail -5
```

Forventet: `Build succeeded. 0 Error(s)`

**Step 4: Commit**

```bash
git add src/TronderLeikan.API/Common/
git commit -m "feat(api): legg til ApiControllerBase med Problem(Error)-overload og ErrorTypeExtensions"
```

---

## Task 6: API-testprosjekt og WebApplicationFactory

**Files:**
- Create: `tests/TronderLeikan.Api.Tests/TronderLeikan.Api.Tests.csproj`
- Create: `tests/TronderLeikan.Api.Tests/GlobalUsings.cs`
- Create: `tests/TronderLeikan.Api.Tests/TronderLeikanApiFactory.cs`
- Create: `tests/TronderLeikan.Api.Tests/ApiTestCollection.cs`

**Step 1: Opprett prosjektfil**

```xml
<!-- tests/TronderLeikan.Api.Tests/TronderLeikan.Api.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.2" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="FluentAssertions" Version="8.2.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <!-- Kun API-referanse — ingen direkte Application-referanse -->
    <ProjectReference Include="../../src/TronderLeikan.API/TronderLeikan.API.csproj" />
    <!-- Infrastructure for AppDbContext i factory-setup -->
    <ProjectReference Include="../../src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Legg til i solution**

```bash
dotnet sln TronderLeikan.slnx add tests/TronderLeikan.Api.Tests/TronderLeikan.Api.Tests.csproj
```

**Step 3: Opprett GlobalUsings.cs**

```csharp
// tests/TronderLeikan.Api.Tests/GlobalUsings.cs
global using System.Net;
global using System.Net.Http.Json;
global using System.Text.Json;
global using FluentAssertions;
global using Xunit;
```

**Step 4: Opprett TronderLeikanApiFactory.cs**

```csharp
// tests/TronderLeikan.Api.Tests/TronderLeikanApiFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TronderLeikan.Infrastructure.Persistence;

namespace TronderLeikan.Api.Tests;

public class TronderLeikanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Injiser Testcontainers-connection string — brukes av AddInfrastructure i Program.cs
        builder.UseSetting(
            "ConnectionStrings:tronderleikan",
            _postgres.GetConnectionString());
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Kjør EF Core-migrasjoner mot ekte PostgreSQL
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

**Step 5: Opprett ApiTestCollection.cs**

```csharp
// tests/TronderLeikan.Api.Tests/ApiTestCollection.cs
namespace TronderLeikan.Api.Tests;

// Alle test-klasser som bruker [Collection(nameof(ApiTestCollection))]
// deler én factory (og én database) per testkjøring
[CollectionDefinition(nameof(ApiTestCollection))]
public class ApiTestCollection : ICollectionFixture<TronderLeikanApiFactory> { }
```

**Step 6: Bygg testprosjektet**

```bash
dotnet build tests/TronderLeikan.Api.Tests/ --no-restore -v quiet 2>&1 | tail -5
```

Forventet: `Build succeeded. 0 Error(s)`

**Step 7: Commit**

```bash
git add tests/TronderLeikan.Api.Tests/
git commit -m "feat(api-tests): sett opp WebApplicationFactory med Testcontainers og xUnit collection fixture"
```

---

## Task 7: DepartmentsController + black-box tester

**Files:**
- Create: `tests/TronderLeikan.Api.Tests/Departments/DepartmentsApiTests.cs`
- Create: `src/TronderLeikan.API/Controllers/DepartmentsController.cs`

**Step 1: Skriv failing tester**

```csharp
// tests/TronderLeikan.Api.Tests/Departments/DepartmentsApiTests.cs
namespace TronderLeikan.Api.Tests.Departments;

[Collection(nameof(ApiTestCollection))]
public class DepartmentsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_departments_returnerer_200_tom_liste()
    {
        var response = await _client.GetAsync("/api/v1/departments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task POST_departments_returnerer_201_med_guid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/departments", new
        {
            name = "IT-avdelingen"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_departments_med_tomt_navn_returnerer_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/departments", new
        {
            name = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Department.NameEmpty");
        body.GetProperty("status").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task Opprett_og_hent_department_happy_path()
    {
        await _client.PostAsJsonAsync("/api/v1/departments", new { name = "Salg" });

        var response = await _client.GetAsync("/api/v1/departments");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.EnumerateArray()
            .Select(d => d.GetProperty("name").GetString())
            .ToList();
        names.Should().Contain("Salg");
    }
}
```

**Step 2: Kjør for å bekrefte at de feiler (404 — ingen controller ennå)**

```bash
dotnet test tests/TronderLeikan.Api.Tests/ --filter "DepartmentsApiTests" --no-build 2>&1 | tail -10
```

Forventet: tester feiler med `Expected: Created, Actual: NotFound` eller liknende.

**Step 3: Opprett DepartmentsController**

```csharp
// src/TronderLeikan.API/Controllers/DepartmentsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Departments.Commands.CreateDepartment;
using TronderLeikan.Application.Departments.Queries.GetDepartments;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class DepartmentsController(
    ICommandHandler<CreateDepartmentCommand, Guid> createHandler,
    IQueryHandler<GetDepartmentsQuery, DepartmentResponse[]> getHandler)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DepartmentResponse[]>> GetAll(CancellationToken ct) =>
        (await getHandler.Handle(new GetDepartmentsQuery(), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetAll), id),
            Problem);
    }
}
```

**Step 4: Bygg og kjør tester**

```bash
dotnet build src/TronderLeikan.API/ --no-restore -v quiet 2>&1 | tail -3
dotnet test tests/TronderLeikan.Api.Tests/ --filter "DepartmentsApiTests" 2>&1 | tail -5
```

Forventet: `Passed! Failed: 0, Passed: 4`

**Step 5: Commit**

```bash
git add src/TronderLeikan.API/Controllers/DepartmentsController.cs tests/TronderLeikan.Api.Tests/Departments/
git commit -m "feat(api): DepartmentsController med black-box akseptansetester"
```

---

## Task 8: PersonsController + black-box tester

**Files:**
- Create: `tests/TronderLeikan.Api.Tests/Persons/PersonsApiTests.cs`
- Create: `src/TronderLeikan.API/Controllers/PersonsController.cs`

**Step 1: Skriv failing tester**

```csharp
// tests/TronderLeikan.Api.Tests/Persons/PersonsApiTests.cs
namespace TronderLeikan.Api.Tests.Persons;

[Collection(nameof(ApiTestCollection))]
public class PersonsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_persons_returnerer_201_med_guid()
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
        body.GetProperty("detail").GetString().Should().Be("Personen finnes ikke.");
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
        body.GetProperty("hasProfileImage").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PUT_person_oppdaterer_navn()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Gammel", lastName = "Navn" }))
            .Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/persons/{id}", new
        {
            personId = id,
            firstName = "Nytt",
            lastName = "Navn"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = await (await _client.GetAsync($"/api/v1/persons/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Nytt");
    }

    [Fact]
    public async Task DELETE_person_returnerer_204()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Slett", lastName = "Meg" }))
            .Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/persons/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_person_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.DeleteAsync($"/api/v1/persons/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_persons_returnerer_liste()
    {
        await _client.PostAsJsonAsync("/api/v1/persons", new { firstName = "Liste", lastName = "Test" });

        var response = await _client.GetAsync("/api/v1/persons");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }
}
```

**Step 2: Kjør for å bekrefte feil**

```bash
dotnet test tests/TronderLeikan.Api.Tests/ --filter "PersonsApiTests" --no-build 2>&1 | tail -5
```

**Step 3: Opprett PersonsController**

```csharp
// src/TronderLeikan.API/Controllers/PersonsController.cs
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Commands.CreatePerson;
using TronderLeikan.Application.Persons.Commands.DeletePerson;
using TronderLeikan.Application.Persons.Commands.DeletePersonImage;
using TronderLeikan.Application.Persons.Commands.UpdatePerson;
using TronderLeikan.Application.Persons.Commands.UploadPersonImage;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class PersonsController(
    ICommandHandler<CreatePersonCommand, Guid> createHandler,
    ICommandHandler<UpdatePersonCommand> updateHandler,
    ICommandHandler<DeletePersonCommand> deleteHandler,
    ICommandHandler<UploadPersonImageCommand> uploadImageHandler,
    ICommandHandler<DeletePersonImageCommand> deleteImageHandler,
    IQueryHandler<GetPersonsQuery, PersonSummaryResponse[]> getPersonsHandler,
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse> getPersonByIdHandler)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PersonSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await getPersonsHandler.Handle(new GetPersonsQuery(), ct)).Match(Ok, Problem);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await getPersonByIdHandler.Handle(new GetPersonByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreatePersonCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdatePersonCommand command, CancellationToken ct) =>
        (await updateHandler.Handle(command with { PersonId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        (await deleteHandler.Handle(new DeletePersonCommand(id), ct)).Match(() => NoContent(), Problem);

    [HttpPut("{id:guid}/image")]
    public async Task<ActionResult> UploadImage(Guid id, IFormFile image, CancellationToken ct)
    {
        using var stream = image.OpenReadStream();
        var result = await uploadImageHandler.Handle(new UploadPersonImageCommand(id, stream), ct);
        return result.Match(() => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult> DeleteImage(Guid id, CancellationToken ct) =>
        (await deleteImageHandler.Handle(new DeletePersonImageCommand(id), ct)).Match(() => NoContent(), Problem);
}
```

**Step 4: Kjør tester**

```bash
dotnet test tests/TronderLeikan.Api.Tests/ --filter "PersonsApiTests" 2>&1 | tail -5
```

Forventet: `Passed! Failed: 0, Passed: 7`

**Step 5: Commit**

```bash
git add src/TronderLeikan.API/Controllers/PersonsController.cs tests/TronderLeikan.Api.Tests/Persons/
git commit -m "feat(api): PersonsController med CRUD og image-endepunkter, black-box tester"
```

---

## Task 9: TournamentsController + black-box tester

**Files:**
- Create: `tests/TronderLeikan.Api.Tests/Tournaments/TournamentsApiTests.cs`
- Create: `src/TronderLeikan.API/Controllers/TournamentsController.cs`

**Step 1: Skriv failing tester**

```csharp
// tests/TronderLeikan.Api.Tests/Tournaments/TournamentsApiTests.cs
namespace TronderLeikan.Api.Tests.Tournaments;

[Collection(nameof(ApiTestCollection))]
public class TournamentsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_tournaments_returnerer_201_med_guid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tournaments", new
        {
            name = "VM 2026",
            slug = "vm-2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_tournament_by_slug_returnerer_turnering()
    {
        await _client.PostAsJsonAsync("/api/v1/tournaments", new { name = "NM 2026", slug = "nm-2026" });

        var response = await _client.GetAsync("/api/v1/tournaments/nm-2026");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("NM 2026");
        body.GetProperty("slug").GetString().Should().Be("nm-2026");
    }

    [Fact]
    public async Task GET_tournament_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync("/api/v1/tournaments/finnes-ikke");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Tournament.NotFound");
    }

    [Fact]
    public async Task PUT_point_rules_oppdaterer_poengregler()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Test", slug = "test-poeng" }))
            .Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PutAsJsonAsync($"/api/v1/tournaments/{id}/point-rules", new
        {
            tournamentId = id,
            participation = 5,
            firstPlace = 15,
            secondPlace = 10,
            thirdPlace = 7,
            organizedWithParticipation = 3,
            organizedWithoutParticipation = 8,
            spectator = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_scoreboard_returnerer_tom_liste_uten_spill()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Scoreboard Test", slug = "scoreboard-test" }))
            .Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync($"/api/v1/tournaments/{id}/scoreboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }
}
```

**Step 2: Opprett TournamentsController**

```csharp
// src/TronderLeikan.API/Controllers/TournamentsController.cs
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Commands.CreateTournament;
using TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;
using TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
using TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
using TronderLeikan.Application.Tournaments.Queries.GetTournaments;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class TournamentsController(
    ICommandHandler<CreateTournamentCommand, Guid> createHandler,
    ICommandHandler<UpdateTournamentPointRulesCommand> pointRulesHandler,
    IQueryHandler<GetTournamentsQuery, TournamentSummaryResponse[]> getTournamentsHandler,
    IQueryHandler<GetTournamentBySlugQuery, TournamentDetailResponse> getBySlugHandler,
    IQueryHandler<GetScoreboardQuery, ScoreboardEntryResponse[]> scoreboardHandler)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TournamentSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await getTournamentsHandler.Handle(new GetTournamentsQuery(), ct)).Match(Ok, Problem);

    // Slug-basert oppslag — brukes av frontend for navigasjon
    [HttpGet("{slug}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetBySlug(string slug, CancellationToken ct) =>
        (await getBySlugHandler.Handle(new GetTournamentBySlugQuery(slug), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTournamentCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetBySlug), new { slug = command.Slug }, id),
            Problem);
    }

    [HttpPut("{id:guid}/point-rules")]
    public async Task<ActionResult> UpdatePointRules(
        Guid id, UpdateTournamentPointRulesCommand command, CancellationToken ct) =>
        (await pointRulesHandler.Handle(command with { TournamentId = id }, ct))
            .Match(() => NoContent(), Problem);

    [HttpGet("{id:guid}/scoreboard")]
    public async Task<ActionResult<ScoreboardEntryResponse[]>> GetScoreboard(Guid id, CancellationToken ct) =>
        (await scoreboardHandler.Handle(new GetScoreboardQuery(id), ct)).Match(Ok, Problem);
}
```

**Step 3: Bygg og kjør tester**

```bash
dotnet test tests/TronderLeikan.Api.Tests/ --filter "TournamentsApiTests" 2>&1 | tail -5
```

Forventet: `Passed! Failed: 0, Passed: 5`

**Step 4: Commit**

```bash
git add src/TronderLeikan.API/Controllers/TournamentsController.cs tests/TronderLeikan.Api.Tests/Tournaments/
git commit -m "feat(api): TournamentsController med slug-oppslag, poengregler og scoreboard"
```

---

## Task 10: GamesController + black-box tester

**Files:**
- Create: `tests/TronderLeikan.Api.Tests/Games/GamesApiTests.cs`
- Create: `src/TronderLeikan.API/Controllers/GamesController.cs`

**Step 1: Skriv failing tester**

```csharp
// tests/TronderLeikan.Api.Tests/Games/GamesApiTests.cs
namespace TronderLeikan.Api.Tests.Games;

[Collection(nameof(ApiTestCollection))]
public class GamesApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Hjelper — oppretter turnering og returnerer id
    private async Task<Guid> OpprettTurnering(string slug) =>
        await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = slug, slug }))
            .Content.ReadFromJsonAsync<Guid>();

    // Hjelper — oppretter person og returnerer id
    private async Task<Guid> OpprettPerson(string fornavn, string etternavn) =>
        await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = fornavn, lastName = etternavn }))
            .Content.ReadFromJsonAsync<Guid>();

    [Fact]
    public async Task POST_games_returnerer_201_med_guid()
    {
        var tournamentId = await OpprettTurnering("games-post-test");

        var response = await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Testspill",
            gameType = 0 // Standard
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_game_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync($"/api/v1/games/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Game.NotFound");
    }

    [Fact]
    public async Task POST_participants_og_complete_game_happy_path()
    {
        var tournamentId = await OpprettTurnering("game-complete-test");
        var personId1 = await OpprettPerson("Per", "Testesen");
        var personId2 = await OpprettPerson("Pål", "Testesen");
        var personId3 = await OpprettPerson("Espen", "Testesen");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Finalespill",
            gameType = 0
        })).Content.ReadFromJsonAsync<Guid>();

        // Legg til deltakere
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId1 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId2 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId3 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fullfør spill
        var completeResponse = await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/complete", new
        {
            gameId,
            firstPlace  = new[] { personId1 },
            secondPlace = new[] { personId2 },
            thirdPlace  = new[] { personId3 }
        });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifiser at spillet er done
        var body = await (await _client.GetAsync($"/api/v1/games/{gameId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isDone").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task POST_simracing_results_og_complete_beregner_plasseringer()
    {
        var tournamentId = await OpprettTurnering("simracing-complete-test");
        var p1 = await OpprettPerson("Rask", "Raser");
        var p2 = await OpprettPerson("Midt", "Raser");
        var p3 = await OpprettPerson("Treg", "Raser");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Simracing 1",
            gameType = 1 // Simracing
        })).Content.ReadFromJsonAsync<Guid>();

        // Registrer racetider (lavest er best)
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p1, raceTimeMs = 90000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p2, raceTimeMs = 95000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p3, raceTimeMs = 100000L });

        // Fullfør automatisk
        var completeResponse = await _client.PostAsync(
            $"/api/v1/games/{gameId}/simracing-results/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifiser at spillet er done
        var body = await (await _client.GetAsync($"/api/v1/games/{gameId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isDone").GetBoolean().Should().BeTrue();

        // Verifiser plasseringer
        var firstPlace = body.GetProperty("firstPlace").EnumerateArray()
            .Select(e => e.GetGuid()).ToList();
        firstPlace.Should().Contain(p1);
    }

    [Fact]
    public async Task GET_simracing_results_returnerer_sortert_liste()
    {
        var tournamentId = await OpprettTurnering("simracing-results-test");
        var p1 = await OpprettPerson("Rask2", "Raser");
        var p2 = await OpprettPerson("Treg2", "Raser");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId, name = "Simracing 2", gameType = 1
        })).Content.ReadFromJsonAsync<Guid>();

        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p2, raceTimeMs = 100000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p1, raceTimeMs = 90000L });

        var response = await _client.GetAsync($"/api/v1/games/{gameId}/simracing-results");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var times = body.EnumerateArray()
            .Select(r => r.GetProperty("raceTimeMs").GetInt64()).ToList();
        times.Should().BeInAscendingOrder();
    }
}
```

**Step 2: Opprett GamesController**

```csharp
// src/TronderLeikan.API/Controllers/GamesController.cs
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
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

public sealed class GamesController(
    ICommandHandler<CreateGameCommand, Guid> createHandler,
    ICommandHandler<UpdateGameCommand> updateHandler,
    ICommandHandler<AddParticipantCommand> addParticipantHandler,
    ICommandHandler<AddOrganizerCommand> addOrganizerHandler,
    ICommandHandler<AddSpectatorCommand> addSpectatorHandler,
    ICommandHandler<CompleteGameCommand> completeHandler,
    ICommandHandler<UploadGameBannerCommand> uploadBannerHandler,
    ICommandHandler<RegisterSimracingResultCommand, Guid> registerSimracingHandler,
    ICommandHandler<CompleteSimracingGameCommand> completeSimracingHandler,
    IQueryHandler<GetGameByIdQuery, GameDetailResponse> getByIdHandler,
    IQueryHandler<GetSimracingResultsQuery, SimracingResultResponse[]> getSimracingHandler)
    : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GameDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await getByIdHandler.Handle(new GetGameByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateGameCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateGameCommand command, CancellationToken ct) =>
        (await updateHandler.Handle(command with { GameId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult> AddParticipant(Guid id, AddParticipantCommand command, CancellationToken ct) =>
        (await addParticipantHandler.Handle(command with { GameId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpPost("{id:guid}/organizers")]
    public async Task<ActionResult> AddOrganizer(Guid id, AddOrganizerCommand command, CancellationToken ct) =>
        (await addOrganizerHandler.Handle(command with { GameId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpPost("{id:guid}/spectators")]
    public async Task<ActionResult> AddSpectator(Guid id, AddSpectatorCommand command, CancellationToken ct) =>
        (await addSpectatorHandler.Handle(command with { GameId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CompleteGameCommand command, CancellationToken ct) =>
        (await completeHandler.Handle(command with { GameId = id }, ct)).Match(() => NoContent(), Problem);

    [HttpPut("{id:guid}/banner")]
    public async Task<ActionResult> UploadBanner(Guid id, IFormFile banner, CancellationToken ct)
    {
        using var stream = banner.OpenReadStream();
        var result = await uploadBannerHandler.Handle(new UploadGameBannerCommand(id, stream), ct);
        return result.Match(() => NoContent(), Problem);
    }

    [HttpGet("{id:guid}/simracing-results")]
    public async Task<ActionResult<SimracingResultResponse[]>> GetSimracingResults(Guid id, CancellationToken ct) =>
        (await getSimracingHandler.Handle(new GetSimracingResultsQuery(id), ct)).Match(Ok, Problem);

    [HttpPost("{id:guid}/simracing-results")]
    public async Task<ActionResult<Guid>> RegisterSimracingResult(
        Guid id, RegisterSimracingResultCommand command, CancellationToken ct)
    {
        var result = await registerSimracingHandler.Handle(command with { GameId = id }, ct);
        return result.Match(
            resultId => CreatedAtAction(nameof(GetSimracingResults), new { id }, resultId),
            Problem);
    }

    [HttpPost("{id:guid}/simracing-results/complete")]
    public async Task<ActionResult> CompleteSimracing(Guid id, CancellationToken ct) =>
        (await completeSimracingHandler.Handle(new CompleteSimracingGameCommand(id), ct))
            .Match(() => NoContent(), Problem);
}
```

**Step 3: Bygg og kjør tester**

```bash
dotnet test tests/TronderLeikan.Api.Tests/ --filter "GamesApiTests" 2>&1 | tail -5
```

Forventet: `Passed! Failed: 0, Passed: 5`

**Step 4: Commit**

```bash
git add src/TronderLeikan.API/Controllers/GamesController.cs tests/TronderLeikan.Api.Tests/Games/
git commit -m "feat(api): GamesController med deltakere, plasseringer og simracing — black-box tester"
```

---

## Task 11: Final bygg og full testkjøring

**Step 1: Bygg hele solution**

```bash
dotnet build --no-restore -v quiet 2>&1 | tail -5
```

Forventet: `Build succeeded. 0 Error(s)` (IgnorableNuGet-advarsler OK)

**Step 2: Kjør alle tester**

```bash
dotnet test 2>&1 | tail -20
```

Forventet:
```
Passed! - Failed: 0, Passed: 28  - TronderLeikan.Domain.Tests.dll
Passed! - Failed: 0, Passed: 24  - TronderLeikan.Application.Tests.dll
Passed! - Failed: 0, Passed:  3  - TronderLeikan.Infrastructure.Tests.dll
Passed! - Failed: 0, Passed: ~21 - TronderLeikan.Api.Tests.dll
```

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat(api): API-lag komplett — controllers, RFC 9457 feilhåndtering, versjonering og akseptansetester"
```
