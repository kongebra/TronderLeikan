# Infrastructure Design — TrønderLeikan

**Dato:** 2026-03-04
**Status:** Godkjent

---

## Kontekst

Infrastructure-laget kobler domenet mot PostgreSQL via EF Core, håndterer bildepersistans (bytea), og sørger for automatisk database-migrering via en dedikert Aspire-migrator. Valkey/cache utelates i dette steget og legges til når Application-laget er på plass.

---

## Valg og begrunnelser

| Valg | Beslutning | Begrunnelse |
|---|---|---|
| EF Core-konfigurasjon | `IEntityTypeConfiguration<T>` per entitet | Rich domain model krever eksplisitt konfigurasjon for backing fields og owned entities. Separate filer er ryddigere enn én stor `OnModelCreating`. |
| Repository-abstraksjon | Ingen — `IAppDbContext` direkte fra Application | Mediator-pattern i Application. DbSet-tilgang er tilstrekkelig. |
| Migrations | Auto-apply via dedikert `DbMigrator`-prosjekt i Aspire | Konsistent med .NET Aspire-mønster, ingen manuelle steg ved oppstart. |
| Bildepersistans | `PersonImage` og `GameBanner` som infrastruktur-entiteter (ikke domenekonsepter) | Bilder er persistansedetaljer, ikke domenelogikk. Separate tabeller unngår at bytes lastes ved vanlige queries. |
| Valkey/Cache | Utsettes | Legges til etter Application-laget når vi vet hva som bør caches. |

---

## Nye prosjekter

### `TronderLeikan.DbMigrator` (ny)

Minimalt console-prosjekt som kjøres av Aspire ved oppstart. Henter `AppDbContext` fra DI og kaller `MigrateAsync()`.

```
src/TronderLeikan.DbMigrator/
├── Program.cs
└── TronderLeikan.DbMigrator.csproj
```

---

## Endringer i eksisterende prosjekter

### `TronderLeikan.Application`

Nytt interface som Infrastructure implementerer:

```
Application/
└── Common/
    └── Interfaces/
        └── IAppDbContext.cs
```

### `TronderLeikan.Infrastructure`

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── DepartmentConfiguration.cs
│   │   ├── PersonConfiguration.cs
│   │   ├── TournamentConfiguration.cs
│   │   ├── GameConfiguration.cs
│   │   └── SimracingResultConfiguration.cs
│   └── Images/
│       ├── PersonImage.cs
│       ├── PersonImageConfiguration.cs
│       ├── GameBanner.cs
│       └── GameBannerConfiguration.cs
└── DependencyInjection.cs
```

### `TronderLeikan.AppHost`

Legger til PostgreSQL, DbMigrator og kobling mellom tjenestene.

---

## IAppDbContext

Definert i **Application** — ikke Infrastructure. Gjør Application uavhengig av EF Core:

```csharp
// Application/Common/Interfaces/IAppDbContext.cs
public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Person> Persons { get; }
    DbSet<Tournament> Tournaments { get; }
    DbSet<Game> Games { get; }
    DbSet<SimracingResult> SimracingResults { get; }
    DbSet<PersonImage> PersonImages { get; }
    DbSet<GameBanner> GameBanners { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## AppDbContext

```csharp
// Infrastructure/Persistence/AppDbContext.cs
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SimracingResult> SimracingResults => Set<SimracingResult>();
    public DbSet<PersonImage> PersonImages => Set<PersonImage>();
    public DbSet<GameBanner> GameBanners => Set<GameBanner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

---

## Bildeentiteter (Infrastructure-only)

Ikke domenekonsepter — defineres i Infrastructure:

```csharp
public sealed class PersonImage
{
    public Guid PersonId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}

public sealed class GameBanner
{
    public Guid GameId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}
```

---

## Entity-konfigurasjon: kritiske punkter

### Game — backing fields og join-tabeller

`Game` bruker private `List<Guid>` backing fields. EF Core må konfigureres til å bruke disse:

```csharp
// GameConfiguration.cs
builder.HasMany<Person>()
    .WithMany()
    .UsingEntity("GameParticipants",
        r => r.HasOne<Person>().WithMany().HasForeignKey("PersonId"),
        l => l.HasOne<Game>().WithMany().HasForeignKey("GameId"))
    .Navigation(g => g.GetType()) // via metadata backing field name
```

Backing field-navn (fra domenet): `_participants`, `_organizers`, `_spectators`, `_firstPlace`, `_secondPlace`, `_thirdPlace`.

EF Core finner backing fields automatisk med `UsePropertyAccessMode(PropertyAccessMode.Field)` per collection.

### TournamentPointRules — owned entity

```csharp
// TournamentConfiguration.cs
builder.OwnsOne(t => t.PointRules, pr =>
{
    pr.Property(p => p.Participation).HasColumnName("PointRules_Participation").HasDefaultValue(3);
    pr.Property(p => p.FirstPlace).HasColumnName("PointRules_FirstPlace").HasDefaultValue(3);
    pr.Property(p => p.SecondPlace).HasColumnName("PointRules_SecondPlace").HasDefaultValue(2);
    pr.Property(p => p.ThirdPlace).HasColumnName("PointRules_ThirdPlace").HasDefaultValue(1);
    pr.Property(p => p.OrganizedWithParticipation).HasColumnName("PointRules_OrgWithParticipation").HasDefaultValue(1);
    pr.Property(p => p.OrganizedWithoutParticipation).HasColumnName("PointRules_OrgWithoutParticipation").HasDefaultValue(3);
    pr.Property(p => p.Spectator).HasColumnName("PointRules_Spectator").HasDefaultValue(1);
});
```

---

## AppHost-oppsett

```csharp
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("tronderleikan");

builder.AddProject<Projects.TronderLeikan_DbMigrator>("migrator")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithReference(postgres)
    .WaitFor("migrator");
```

---

## DependencyInjection

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString) =>
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString))
        .AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
}
```

---

## NuGet-pakker som trengs

| Prosjekt | Pakke |
|---|---|
| Infrastructure | `Microsoft.EntityFrameworkCore` |
| Infrastructure | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Infrastructure | `Microsoft.EntityFrameworkCore.Relational` |
| DbMigrator | `Microsoft.EntityFrameworkCore.Design` |
| AppHost | `CommunityToolkit.Aspire.Hosting.Bun` (allerede) |
| AppHost | `Aspire.Hosting.PostgreSQL` |

---

## Databaseskjema (resulterende tabeller)

| Tabell | Beskrivelse |
|---|---|
| `Departments` | Id, Name |
| `Persons` | Id, FirstName, LastName, DepartmentId (FK), HasProfileImage |
| `PersonImages` | PersonId (PK/FK), ImageData, ContentType |
| `Tournaments` | Id, Name, Slug, PointRules_* (7 kolonner) |
| `Games` | Id, TournamentId (FK), Name, Description, IsDone, GameType, IsOrganizersParticipating, HasBanner |
| `GameBanners` | GameId (PK/FK), ImageData, ContentType |
| `GameParticipants` | GameId, PersonId |
| `GameOrganizers` | GameId, PersonId |
| `GameSpectators` | GameId, PersonId |
| `GameFirstPlace` | GameId, PersonId |
| `GameSecondPlace` | GameId, PersonId |
| `GameThirdPlace` | GameId, PersonId |
| `SimracingResults` | Id, GameId (FK), PersonId (FK), RaceTimeMs |
