# Infrastructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Koble domenet mot PostgreSQL via EF Core, sett opp bildepersistans i egne tabeller, opprett DbMigrator-prosjekt for automatiske migrations, og registrer alt i Aspire AppHost.

**Architecture:** `IAppDbContext` defineres i Application (uavhengig av EF Core). `AppDbContext` implementerer den i Infrastructure. Game-entitetens `List<Guid>` backing fields mappes til native PostgreSQL `uuid[]`-kolonner via Npgsql. TournamentPointRules konfigureres som EF Core `OwnsOne`. Bilder lagres i separate tabeller (`PersonImages`, `GameBanners`) som infrastruktur-entiteter — ikke domenekonsepter. Migrations genereres og kjøres automatisk av `TronderLeikan.DbMigrator` ved Aspire-oppstart.

**Tech Stack:** .NET 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, .NET Aspire 13, xUnit + FluentAssertions + Testcontainers.PostgreSQL (integrasjonstester)

---

## Viktige EF Core-mønstre brukt i denne planen

### Game backing fields → PostgreSQL uuid[] arrays
Game-entiteten har private `List<Guid>` backing fields (`_participants`, `_organizers` osv.). Disse mappes som native PostgreSQL array-kolonner via Npgsql:
```csharp
builder.Property<List<Guid>>("_participants")
    .HasColumnName("Participants")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
// Npgsql mapper List<Guid> automatisk til uuid[] i PostgreSQL
```

### TournamentPointRules → OwnsOne med flate kolonner
```csharp
builder.OwnsOne(t => t.PointRules, pr => {
    pr.Property(p => p.Participation).HasColumnName("PointRules_Participation");
});
```

### Bilder → infrastruktur-entiteter, ikke domenekonsepter
`PersonImage` og `GameBanner` finnes kun i Infrastructure, ikke i Domain.

---

## Task 1: NuGet-pakker og prosjektreferanser

**Files:**
- Modify: `src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj`
- Modify: `src/TronderLeikan.Application/TronderLeikan.Application.csproj`

**Steg 1: Legg til pakker i Infrastructure**
```bash
cd /Users/svedanie/crayon/TronderLeikan/src/TronderLeikan.Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Steg 2: Legg til EF Core abstraksjon i Application (for IAppDbContext)**
```bash
cd /Users/svedanie/crayon/TronderLeikan/src/TronderLeikan.Application
dotnet add package Microsoft.EntityFrameworkCore
```

**Steg 3: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```
Forventet: `Build succeeded`

**Steg 4: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
git add src/TronderLeikan.Application/TronderLeikan.Application.csproj
git commit -m "chore: legg til EF Core og Npgsql NuGet-pakker"
```

---

## Task 2: IAppDbContext i Application + bildeentiteter i Infrastructure

**Files:**
- Create: `src/TronderLeikan.Application/Common/Interfaces/IAppDbContext.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Images/PersonImage.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Images/GameBanner.cs`

**Steg 1: Opprett IAppDbContext**

Opprett `src/TronderLeikan.Application/Common/Interfaces/IAppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;
using TronderLeikan.Infrastructure.Persistence.Images;

namespace TronderLeikan.Application.Common.Interfaces;

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

**Steg 2: Merk — IAppDbContext refererer til Infrastructure.Persistence.Images**

Dette er et bevisst valg: PersonImage og GameBanner er persistansedetaljer som Application kjenner til, men ikke Domain. Application kan dermed lese/skrive bilder via DbContext uten å ta inn Domain-avhengighet.

**Steg 3: Opprett PersonImage**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Images/PersonImage.cs`:

```csharp
namespace TronderLeikan.Infrastructure.Persistence.Images;

// Infrastruktur-entitet — ikke et domenekonsept
// Bilder holdes i en separat tabell for å unngå at bytes lastes ved vanlige Person-queries
public sealed class PersonImage
{
    public Guid PersonId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}
```

**Steg 4: Opprett GameBanner**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Images/GameBanner.cs`:

```csharp
namespace TronderLeikan.Infrastructure.Persistence.Images;

// Infrastruktur-entitet — ikke et domenekonsept
// Game-bannere lagres separat (opptil 1920x1080 WebP) for å unngå at bytes lastes ved Game-queries
public sealed class GameBanner
{
    public Guid GameId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}
```

**Steg 5: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```

**Steg 6: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Application/Common/ src/TronderLeikan.Infrastructure/Persistence/Images/
git commit -m "feat: legg til IAppDbContext og bildeentiteter (PersonImage, GameBanner)"
```

---

## Task 3: AppDbContext og IDesignTimeDbContextFactory

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/AppDbContextFactory.cs`

**Steg 1: Opprett AppDbContext**

Opprett `src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;
using TronderLeikan.Infrastructure.Persistence.Images;

namespace TronderLeikan.Infrastructure.Persistence;

// internal: AppDbContext er en implementasjonsdetalj — Application bruker IAppDbContext
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

**Steg 2: Opprett IDesignTimeDbContextFactory**

Denne brukes utelukkende av EF Core CLI-verktøy (`dotnet ef migrations add`). Den kjøres aldri i produksjon.

Opprett `src/TronderLeikan.Infrastructure/Persistence/AppDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TronderLeikan.Infrastructure.Persistence;

// Brukes kun av EF Core design-time verktøy (dotnet ef migrations)
// Ikke registrert i DI — kun for CLI
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=tronderleikan_dev;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
```

**Steg 3: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```
Forventet: `Build succeeded`

**Steg 4: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Persistence/AppDbContext.cs
git add src/TronderLeikan.Infrastructure/Persistence/AppDbContextFactory.cs
git commit -m "feat: legg til AppDbContext og IDesignTimeDbContextFactory"
```

---

## Task 4: DepartmentConfiguration og PersonConfiguration

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/PersonConfiguration.cs`

**Steg 1: Opprett DepartmentConfiguration**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Departments;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);
        builder.HasIndex(d => d.Name).IsUnique();
        builder.ToTable("Departments");
    }
}
```

**Steg 2: Opprett PersonConfiguration**

Person.Name er private setter — EF Core trenger backing field-konfigurasjon for `FirstName`/`LastName`. Med `private set` fungerer EF Core automatisk via property access. `DepartmentId` er nullable FK.

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/PersonConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(p => p.DepartmentId);
        builder.Property(p => p.HasProfileImage);

        // Avdeling er valgfri — null betyr ingen avdeling satt ennå
        builder.HasOne<TronderLeikan.Domain.Departments.Department>()
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.ToTable("Persons");
    }
}
```

**Steg 3: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Steg 4: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Persistence/Configurations/
git commit -m "feat: legg til DepartmentConfiguration og PersonConfiguration"
```

---

## Task 5: TournamentConfiguration

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/TournamentConfiguration.cs`

**Steg 1: Opprett TournamentConfiguration**

`TournamentPointRules` er et owned value object — EF Core flater det ut i Tournament-tabellen som separate kolonner.

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/TournamentConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(200);
        builder.HasIndex(t => t.Slug).IsUnique();

        // TournamentPointRules er et owned value object — flates ut i Tournament-tabellen
        // Alle poengregler lagres som kolonner direkte på Tournaments-tabellen
        builder.OwnsOne(t => t.PointRules, pr =>
        {
            pr.Property(p => p.Participation)
                .HasColumnName("PointRules_Participation")
                .HasDefaultValue(3);
            pr.Property(p => p.FirstPlace)
                .HasColumnName("PointRules_FirstPlace")
                .HasDefaultValue(3);
            pr.Property(p => p.SecondPlace)
                .HasColumnName("PointRules_SecondPlace")
                .HasDefaultValue(2);
            pr.Property(p => p.ThirdPlace)
                .HasColumnName("PointRules_ThirdPlace")
                .HasDefaultValue(1);
            pr.Property(p => p.OrganizedWithParticipation)
                .HasColumnName("PointRules_OrgWithParticipation")
                .HasDefaultValue(1);
            pr.Property(p => p.OrganizedWithoutParticipation)
                .HasColumnName("PointRules_OrgWithoutParticipation")
                .HasDefaultValue(3);
            pr.Property(p => p.Spectator)
                .HasColumnName("PointRules_Spectator")
                .HasDefaultValue(1);
        });

        builder.ToTable("Tournaments");
    }
}
```

**Steg 2: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Steg 3: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Persistence/Configurations/TournamentConfiguration.cs
git commit -m "feat: legg til TournamentConfiguration med OwnsOne for PointRules"
```

---

## Task 6: GameConfiguration

Dette er den mest komplekse konfigurasjonen. Game har 6 private `List<Guid>` backing fields som mappes til native PostgreSQL `uuid[]`-kolonner via Npgsql.

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/GameConfiguration.cs`

**Steg 1: Opprett GameConfiguration**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/GameConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.TournamentId).IsRequired();
        builder.Property(g => g.Name).IsRequired().HasMaxLength(500);
        builder.Property(g => g.Description).HasMaxLength(5000);
        builder.Property(g => g.IsDone);
        builder.Property(g => g.GameType);
        builder.Property(g => g.IsOrganizersParticipating);
        builder.Property(g => g.HasBanner);

        builder.HasIndex(g => g.TournamentId);

        // Game lagres med alle personlister som native PostgreSQL uuid[]-kolonner.
        // Backing fields er private List<Guid> i Game-entiteten.
        // Npgsql mapper List<Guid> automatisk til uuid[] i PostgreSQL.
        // PropertyAccessMode.Field forteller EF Core å lese/skrive direkte mot feltet, ikke property.

        builder.Property<List<Guid>>("_participants")
            .HasColumnName("Participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_organizers")
            .HasColumnName("Organizers")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_spectators")
            .HasColumnName("Spectators")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_firstPlace")
            .HasColumnName("FirstPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_secondPlace")
            .HasColumnName("SecondPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_thirdPlace")
            .HasColumnName("ThirdPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.ToTable("Games");
    }
}
```

**Steg 2: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```
Forventet: `Build succeeded`

**Steg 3: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Persistence/Configurations/GameConfiguration.cs
git commit -m "feat: legg til GameConfiguration med uuid[] backing fields"
```

---

## Task 7: SimracingResultConfiguration, PersonImageConfiguration, GameBannerConfiguration

**Files:**
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/SimracingResultConfiguration.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/PersonImageConfiguration.cs`
- Create: `src/TronderLeikan.Infrastructure/Persistence/Configurations/GameBannerConfiguration.cs`

**Steg 1: Opprett SimracingResultConfiguration**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/SimracingResultConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class SimracingResultConfiguration : IEntityTypeConfiguration<SimracingResult>
{
    public void Configure(EntityTypeBuilder<SimracingResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.GameId).IsRequired();
        builder.Property(r => r.PersonId).IsRequired();
        builder.Property(r => r.RaceTimeMs).IsRequired();

        // Indeks for rask henting av alle tider for et spill
        builder.HasIndex(r => r.GameId);
        // Unik per person per spill — én tid per løper
        builder.HasIndex(r => new { r.GameId, r.PersonId }).IsUnique();

        builder.ToTable("SimracingResults");
    }
}
```

**Steg 2: Opprett PersonImageConfiguration**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/PersonImageConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Images;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class PersonImageConfiguration : IEntityTypeConfiguration<PersonImage>
{
    public void Configure(EntityTypeBuilder<PersonImage> builder)
    {
        // PersonId er både PK og FK til Persons — 1-til-1 relasjon
        builder.HasKey(i => i.PersonId);
        builder.Property(i => i.ImageData).IsRequired();
        builder.Property(i => i.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("image/webp");

        builder.ToTable("PersonImages");
    }
}
```

**Steg 3: Opprett GameBannerConfiguration**

Opprett `src/TronderLeikan.Infrastructure/Persistence/Configurations/GameBannerConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Images;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class GameBannerConfiguration : IEntityTypeConfiguration<GameBanner>
{
    public void Configure(EntityTypeBuilder<GameBanner> builder)
    {
        // GameId er både PK og FK til Games — 1-til-1 relasjon
        builder.HasKey(b => b.GameId);
        builder.Property(b => b.ImageData).IsRequired();
        builder.Property(b => b.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("image/webp");

        builder.ToTable("GameBanners");
    }
}
```

**Steg 4: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Steg 5: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Persistence/Configurations/
git commit -m "feat: legg til SimracingResult-, PersonImage- og GameBannerConfiguration"
```

---

## Task 8: DependencyInjection.cs og slett Class1.cs

**Files:**
- Create: `src/TronderLeikan.Infrastructure/DependencyInjection.cs`
- Delete: `src/TronderLeikan.Infrastructure/Class1.cs`

**Steg 1: Opprett DependencyInjection**

Opprett `src/TronderLeikan.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;

namespace TronderLeikan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Application bruker IAppDbContext — aldri AppDbContext direkte
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
```

**Steg 2: Slett placeholder**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git rm src/TronderLeikan.Infrastructure/Class1.cs
```

**Steg 3: Verifiser at hele solution bygger**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```

**Steg 4: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/DependencyInjection.cs
git commit -m "feat: legg til Infrastructure DependencyInjection og slett Class1"
```

---

## Task 9: Opprett TronderLeikan.DbMigrator

**Files:**
- Create: `src/TronderLeikan.DbMigrator/TronderLeikan.DbMigrator.csproj`
- Create: `src/TronderLeikan.DbMigrator/Program.cs`

**Steg 1: Opprett console-prosjekt**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet new console -n TronderLeikan.DbMigrator -o src/TronderLeikan.DbMigrator
```

**Steg 2: Legg til referanser**
```bash
cd /Users/svedanie/crayon/TronderLeikan/src/TronderLeikan.DbMigrator
dotnet add reference ../TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.Extensions.Hosting
```

**Steg 3: Legg til i solution og Aspire**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet sln TronderLeikan.slnx add src/TronderLeikan.DbMigrator/TronderLeikan.DbMigrator.csproj
```

DbMigrator.csproj skal bruke Aspire SDK — endre `<Project Sdk="Microsoft.NET.Sdk">` til:

Åpne `src/TronderLeikan.DbMigrator/TronderLeikan.DbMigrator.csproj` og sett innhold:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UserSecretsId>tronderleikan-dbmigrator</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj" />
    <ProjectReference Include="../TronderLeikan.ServiceDefaults/TronderLeikan.ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.3.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**Steg 4: Skriv Program.cs**

Erstatt innholdet i `src/TronderLeikan.DbMigrator/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TronderLeikan.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Aspire ServiceDefaults gir helse-sjekk og telemetri
builder.AddServiceDefaults();

// Npgsql-kobling via Aspire connection string "tronderleikan"
builder.AddNpgsqlDbContext<TronderLeikan.Infrastructure.Persistence.AppDbContext>("tronderleikan");

var host = builder.Build();

// Kjør alle ventende migrations mot databasen og avslutt
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<TronderLeikan.Infrastructure.Persistence.AppDbContext>();
await db.Database.MigrateAsync();

Console.WriteLine("Migrations fullfort.");
```

**Merk:** `AppDbContext` er `internal` i Infrastructure. Vi trenger å gjøre den tilgjengelig for DbMigrator. Legg til i Infrastructure.csproj:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="TronderLeikan.DbMigrator" />
</ItemGroup>
```

**Steg 5: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```

**Steg 6: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.DbMigrator/ src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj TronderLeikan.slnx
git commit -m "feat: legg til DbMigrator-prosjekt for automatiske EF Core migrations"
```

---

## Task 10: Oppdater AppHost

**Files:**
- Modify: `src/TronderLeikan.AppHost/AppHost.cs`
- Modify: `src/TronderLeikan.AppHost/TronderLeikan.AppHost.csproj`

**Steg 1: Legg til prosjektreferanse i AppHost.csproj**

Åpne `src/TronderLeikan.AppHost/TronderLeikan.AppHost.csproj` og legg til DbMigrator-referanse:

```xml
<ItemGroup>
  <ProjectReference Include="../TronderLeikan.API/TronderLeikan.API.csproj" />
  <ProjectReference Include="../TronderLeikan.DbMigrator/TronderLeikan.DbMigrator.csproj" />
</ItemGroup>
```

**Steg 2: Oppdater AppHost.cs**

Erstatt innholdet i `src/TronderLeikan.AppHost/AppHost.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — database for TrønderLeikan
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("tronderleikan");

// DbMigrator kjøres automatisk ved oppstart, etter at PostgreSQL er klar
// API venter til migrations er fullfort
var migrator = builder.AddProject<Projects.TronderLeikan_DbMigrator>("migrator")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithReference(postgres)
    .WaitFor(migrator)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
```

**Steg 3: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```
Forventet: `Build succeeded`

**Steg 4: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.AppHost/AppHost.cs src/TronderLeikan.AppHost/TronderLeikan.AppHost.csproj
git commit -m "feat: oppdater AppHost med PostgreSQL og DbMigrator i Aspire"
```

---

## Task 11: Generer initial EF Core migration

**Steg 1: Generer migration**

EF Core CLI bruker `IDesignTimeDbContextFactory` i Infrastructure til å instansiere DbContext uten en kjørende app.

```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet ef migrations add InitialCreate --project src/TronderLeikan.Infrastructure
```

Forventet output:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

En ny mappe `src/TronderLeikan.Infrastructure/Migrations/` skal oppstå med:
- `<timestamp>_InitialCreate.cs`
- `<timestamp>_InitialCreate.Designer.cs`
- `AppDbContextModelSnapshot.cs`

**Steg 2: Inspiser migrasjonen**

Åpne den genererte `*_InitialCreate.cs` og verifiser:
- `Departments`-tabell med `Name`-kolonne
- `Persons`-tabell med `DepartmentId` FK
- `Tournaments`-tabell med `PointRules_*`-kolonner (7 stk)
- `Games`-tabell med `Participants`, `Organizers`, `Spectators`, `FirstPlace`, `SecondPlace`, `ThirdPlace` som `uuid[]`
- `SimracingResults`-tabell med unik indeks på `(GameId, PersonId)`
- `PersonImages` og `GameBanners` tabeller

**Steg 3: Commit migration**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.Infrastructure/Migrations/
git commit -m "feat: legg til initial EF Core migration (InitialCreate)"
```

---

## Task 12: Registrer Infrastructure i API og oppdater Program.cs

**Files:**
- Modify: `src/TronderLeikan.API/Program.cs`

**Steg 1: Oppdater API Program.cs**

Erstatt innholdet i `src/TronderLeikan.API/Program.cs`:

```csharp
using TronderLeikan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Kobling til PostgreSQL via Aspire — connection string hentes fra environment
var connectionString = builder.Configuration.GetConnectionString("tronderleikan")
    ?? throw new InvalidOperationException("Connection string 'tronderleikan' ikke konfigurert.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.Run();
```

**Steg 2: Verifiser bygg**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```

**Steg 3: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add src/TronderLeikan.API/Program.cs
git commit -m "feat: registrer Infrastructure i API Program.cs"
```

---

## Task 13: Integrasjonstest — verifiser Game-konfigurasjon med Testcontainers

Vi bruker Testcontainers.PostgreSQL for å verifisere at Game-entiteten (med `uuid[]` backing fields) faktisk kan lagres og hentes fra en ekte PostgreSQL-database. InMemory støtter ikke PostgreSQL-spesifikke typer.

**Files:**
- Create: `tests/TronderLeikan.Infrastructure.Tests/TronderLeikan.Infrastructure.Tests.csproj`
- Create: `tests/TronderLeikan.Infrastructure.Tests/GamePersistenceTests.cs`

**Steg 1: Opprett testprosjekt**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet new xunit -n TronderLeikan.Infrastructure.Tests -o tests/TronderLeikan.Infrastructure.Tests
cd tests/TronderLeikan.Infrastructure.Tests
dotnet add package FluentAssertions
dotnet add package Testcontainers.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add reference ../../src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj
```

**Steg 2: Legg til i solution og InternalsVisibleTo**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet sln TronderLeikan.slnx add tests/TronderLeikan.Infrastructure.Tests/TronderLeikan.Infrastructure.Tests.csproj
```

Legg til i `src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj`:
```xml
<ItemGroup>
  <InternalsVisibleTo Include="TronderLeikan.DbMigrator" />
  <InternalsVisibleTo Include="TronderLeikan.Infrastructure.Tests" />
</ItemGroup>
```

**Steg 3: Skriv testene (de vil feile inntil alt er koblet opp)**

Opprett `tests/TronderLeikan.Infrastructure.Tests/GamePersistenceTests.cs`:

```csharp
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSQL;
using TronderLeikan.Domain.Games;
using TronderLeikan.Infrastructure.Persistence;

namespace TronderLeikan.Infrastructure.Tests;

// Disse testene krever Docker kjørende på maskinen
public sealed class GamePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Game_MedDeltakere_KanLagresOgHentes()
    {
        // Migrer databasen
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Opprett et spill med deltakere
        var tournamentId = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var game = Game.Create("Kubb", tournamentId);
        game.AddParticipant(alice);
        game.AddParticipant(bob);

        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Hent spillet fra databasen
        var lagretGame = await context.Games.FindAsync(game.Id);

        lagretGame.Should().NotBeNull();
        lagretGame!.Participants.Should().HaveCount(2)
            .And.Contain(alice)
            .And.Contain(bob);
    }

    [Fact]
    public async Task Game_Complete_LagrerPlasseringer()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var charlie = Guid.NewGuid();

        var game = Game.Create("Dart", Guid.NewGuid());
        game.Complete(
            firstPlace: [alice],
            secondPlace: [bob],
            thirdPlace: [charlie]
        );

        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Last på nytt fra DB
        context.ChangeTracker.Clear();
        var lagretGame = await context.Games.FindAsync(game.Id);

        lagretGame!.IsDone.Should().BeTrue();
        lagretGame.FirstPlace.Should().ContainSingle().Which.Should().Be(alice);
        lagretGame.SecondPlace.Should().ContainSingle().Which.Should().Be(bob);
        lagretGame.ThirdPlace.Should().ContainSingle().Which.Should().Be(charlie);
    }

    [Fact]
    public async Task Tournament_MedPointRules_KanLagresOgHentes()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var tournament = TronderLeikan.Domain.Tournaments.Tournament.Create("Høst-leikan", "host-leikan");
        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var lagretTournament = await context.Tournaments.FindAsync(tournament.Id);

        lagretTournament!.PointRules.Participation.Should().Be(3);
        lagretTournament.PointRules.FirstPlace.Should().Be(3);
    }
}
```

**Steg 4: Kjør testene**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet test tests/TronderLeikan.Infrastructure.Tests/
```

Forventet: Alle 3 tester PASS (krever Docker kjørende).

Hvis Docker ikke kjører: testene feiler med `DockerNotAvailableException` — start Docker og prøv igjen.

**Steg 5: Commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add tests/TronderLeikan.Infrastructure.Tests/ src/TronderLeikan.Infrastructure/TronderLeikan.Infrastructure.csproj TronderLeikan.slnx
git commit -m "test: legg til Infrastructure integrasjonstester med Testcontainers"
```

---

## Task 14: Full bygg-sjekk og opprydding

**Steg 1: Kjør alle tester**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet test tests/TronderLeikan.Domain.Tests/
dotnet test tests/TronderLeikan.Infrastructure.Tests/
```
Forventet: 29 domenestester + 3 infrastrukturtester = 32 tester PASS.

**Steg 2: Bygg hele solution**
```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet build TronderLeikan.slnx
```
Forventet: `Build succeeded. 0 Warning(s). 0 Error(s).`

**Steg 3: Endelig commit**
```bash
cd /Users/svedanie/crayon/TronderLeikan
git add .
git commit -m "feat: Infrastructure-laget fullstendig implementert med EF Core, migrations og integrasjonstester"
```

---

## Filstruktur etter ferdigstilling

```
src/TronderLeikan.Application/
└── Common/
    └── Interfaces/
        └── IAppDbContext.cs

src/TronderLeikan.Infrastructure/
├── DependencyInjection.cs
├── Migrations/
│   ├── <timestamp>_InitialCreate.cs
│   └── AppDbContextModelSnapshot.cs
└── Persistence/
    ├── AppDbContext.cs
    ├── AppDbContextFactory.cs (kun for EF Core design tools)
    ├── Configurations/
    │   ├── DepartmentConfiguration.cs
    │   ├── PersonConfiguration.cs
    │   ├── TournamentConfiguration.cs
    │   ├── GameConfiguration.cs
    │   ├── SimracingResultConfiguration.cs
    │   ├── PersonImageConfiguration.cs
    │   └── GameBannerConfiguration.cs
    └── Images/
        ├── PersonImage.cs
        └── GameBanner.cs

src/TronderLeikan.DbMigrator/
├── Program.cs
└── TronderLeikan.DbMigrator.csproj

tests/TronderLeikan.Infrastructure.Tests/
└── GamePersistenceTests.cs
```

---

## Databaseskjema (resulterende tabeller)

| Tabell | Nøkkelkolonner |
|---|---|
| `Departments` | Id, Name (unique) |
| `Persons` | Id, FirstName, LastName, DepartmentId (FK nullable), HasProfileImage |
| `PersonImages` | PersonId (PK/FK), ImageData, ContentType |
| `Tournaments` | Id, Name, Slug (unique), PointRules_* (7 kolonner) |
| `Games` | Id, TournamentId (FK), Name, Description, IsDone, GameType, IsOrganizersParticipating, HasBanner, Participants (uuid[]), Organizers (uuid[]), Spectators (uuid[]), FirstPlace (uuid[]), SecondPlace (uuid[]), ThirdPlace (uuid[]) |
| `GameBanners` | GameId (PK/FK), ImageData, ContentType |
| `SimracingResults` | Id, GameId (FK), PersonId, RaceTimeMs, unique(GameId+PersonId) |
