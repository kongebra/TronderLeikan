# Domain Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Bygge domenet for TrønderLeikan — entiteter, domenehendelser og forretningsregler — fra tom `Class1.cs` til fullstendig domenelayer.

**Architecture:** Rich domain model med private setters og factory-metoder. Entiteter arver fra felles `Entity`-basisklasse som holder domenehendelser. Mange-til-mange-relasjoner (deltakere, arrangører, plasseringer) representeres som `List<Guid>` i entitetene — EF Core-mapping konfigureres i Infrastructure.

**Tech Stack:** .NET 10, C# 13 (primary constructors der det passer), xUnit 2, FluentAssertions 6

---

## Før du starter

Les designdokumentet: `docs/plans/2026-03-04-domain-design.md`

Alle tester kjøres fra repo-roten:
```bash
dotnet test tests/TronderLeikan.Domain.Tests/TronderLeikan.Domain.Tests.csproj
```

---

## Task 1: Sett opp testprosjekt

**Files:**
- Create: `tests/TronderLeikan.Domain.Tests/TronderLeikan.Domain.Tests.csproj`

**Steg 1: Opprett xUnit-testprosjekt**

```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet new xunit -n TronderLeikan.Domain.Tests -o tests/TronderLeikan.Domain.Tests
```

**Steg 2: Legg til FluentAssertions og prosjektreferanse**

```bash
cd tests/TronderLeikan.Domain.Tests
dotnet add package FluentAssertions
dotnet add reference ../../src/TronderLeikan.Domain/TronderLeikan.Domain.csproj
```

**Steg 3: Legg til testprosjektet i solution**

```bash
cd /Users/svedanie/crayon/TronderLeikan
dotnet sln TronderLeikan.slnx add tests/TronderLeikan.Domain.Tests/TronderLeikan.Domain.Tests.csproj
```

**Steg 4: Verifiser at bygget er grønt**

```bash
dotnet build tests/TronderLeikan.Domain.Tests/TronderLeikan.Domain.Tests.csproj
```
Forventet: `Build succeeded`

**Steg 5: Commit**

```bash
git add tests/
git add TronderLeikan.slnx
git commit -m "chore: legg til domenetesteprosjekt med xUnit og FluentAssertions"
```

---

## Task 2: Basis-infrastruktur — Entity og IDomainEvent

**Files:**
- Create: `src/TronderLeikan.Domain/Common/IDomainEvent.cs`
- Create: `src/TronderLeikan.Domain/Common/Entity.cs`
- Delete: `src/TronderLeikan.Domain/Class1.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Common/EntityTests.cs`

**Steg 1: Skriv testene (kjør dem og se dem feile)**

Opprett `tests/TronderLeikan.Domain.Tests/Common/EntityTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Tests.Common;

// Konkret testklasse siden Entity er abstrakt
file sealed class TestEntity : Entity
{
    private TestEntity() { }

    public static TestEntity Create() => new() { Id = Guid.NewGuid() };

    public void RaiseDomainEvent(IDomainEvent @event) => AddDomainEvent(@event);
}

file sealed record TestEvent : IDomainEvent;

public class EntityTests
{
    [Fact]
    public void NyEntity_HarIngenDomeneHendelser()
    {
        var entity = TestEntity.Create();
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_LeggTilHendelse()
    {
        var entity = TestEntity.Create();
        var @event = new TestEvent();

        entity.RaiseDomainEvent(@event);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(@event);
    }

    [Fact]
    public void ClearDomainEvents_FjernerAlleHendelser()
    {
        var entity = TestEntity.Create();
        entity.RaiseDomainEvent(new TestEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~EntityTests"
```
Forventet: FAIL — `IDomainEvent` og `Entity` finnes ikke ennå.

**Steg 3: Implementer IDomainEvent**

Opprett `src/TronderLeikan.Domain/Common/IDomainEvent.cs`:

```csharp
namespace TronderLeikan.Domain.Common;

public interface IDomainEvent;
```

**Steg 4: Implementer Entity**

Opprett `src/TronderLeikan.Domain/Common/Entity.cs`:

```csharp
namespace TronderLeikan.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**Steg 5: Slett placeholder**

```bash
rm src/TronderLeikan.Domain/Class1.cs
rm src/TronderLeikan.Application/Class1.cs
```

**Steg 6: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~EntityTests"
```
Forventet: PASS — 3 tester grønne.

**Steg 7: Commit**

```bash
git add src/TronderLeikan.Domain/ src/TronderLeikan.Application/ tests/
git commit -m "feat: legg til Entity-basisklasse og IDomainEvent"
```

---

## Task 3: Department-entitet

**Files:**
- Create: `src/TronderLeikan.Domain/Departments/Department.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Departments/DepartmentTests.cs`

**Steg 1: Skriv testene**

Opprett `tests/TronderLeikan.Domain.Tests/Departments/DepartmentTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Departments;

namespace TronderLeikan.Domain.Tests.Departments;

public class DepartmentTests
{
    [Fact]
    public void Create_SetsNameOgId()
    {
        var department = Department.Create("Teknologi");

        department.Id.Should().NotBeEmpty();
        department.Name.Should().Be("Teknologi");
    }

    [Fact]
    public void Rename_EndrerNavn()
    {
        var department = Department.Create("Gammelt navn");

        department.Rename("Nytt navn");

        department.Name.Should().Be("Nytt navn");
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~DepartmentTests"
```

**Steg 3: Implementer Department**

Opprett `src/TronderLeikan.Domain/Departments/Department.cs`:

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Departments;

public sealed class Department : Entity
{
    private Department() { }

    public string Name { get; private set; } = string.Empty;

    public static Department Create(string name) =>
        new() { Id = Guid.NewGuid(), Name = name };

    public void Rename(string name) => Name = name;
}
```

**Steg 4: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~DepartmentTests"
```

**Steg 5: Commit**

```bash
git add src/TronderLeikan.Domain/Departments/ tests/TronderLeikan.Domain.Tests/Departments/
git commit -m "feat: legg til Department-entitet"
```

---

## Task 4: Person-entitet

**Files:**
- Create: `src/TronderLeikan.Domain/Persons/Person.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Persons/PersonTests.cs`

**Steg 1: Skriv testene**

Opprett `tests/TronderLeikan.Domain.Tests/Persons/PersonTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Domain.Tests.Persons;

public class PersonTests
{
    [Fact]
    public void Create_SetsNavnOgId()
    {
        var person = Person.Create("Ola", "Nordmann");

        person.Id.Should().NotBeEmpty();
        person.FirstName.Should().Be("Ola");
        person.LastName.Should().Be("Nordmann");
        person.DepartmentId.Should().BeNull();
        person.HasProfileImage.Should().BeFalse();
    }

    [Fact]
    public void Create_MedAvdeling_SetsDepartmentId()
    {
        var departmentId = Guid.NewGuid();

        var person = Person.Create("Kari", "Nordmann", departmentId);

        person.DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public void SetProfileImage_SetsHasProfileImageTilTrue()
    {
        var person = Person.Create("Ola", "Nordmann");

        person.SetProfileImage();

        person.HasProfileImage.Should().BeTrue();
    }

    [Fact]
    public void RemoveProfileImage_SetsHasProfileImageTilFalse()
    {
        var person = Person.Create("Ola", "Nordmann");
        person.SetProfileImage();

        person.RemoveProfileImage();

        person.HasProfileImage.Should().BeFalse();
    }

    [Fact]
    public void UpdateDepartment_EndrerAvdeling()
    {
        var person = Person.Create("Ola", "Nordmann");
        var nyAvdeling = Guid.NewGuid();

        person.UpdateDepartment(nyAvdeling);

        person.DepartmentId.Should().Be(nyAvdeling);
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~PersonTests"
```

**Steg 3: Implementer Person**

Opprett `src/TronderLeikan.Domain/Persons/Person.cs`:

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Persons;

public sealed class Person : Entity
{
    private Person() { }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public bool HasProfileImage { get; private set; }

    public static Person Create(string firstName, string lastName, Guid? departmentId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DepartmentId = departmentId
        };

    public void SetProfileImage() => HasProfileImage = true;
    public void RemoveProfileImage() => HasProfileImage = false;
    public void UpdateDepartment(Guid? departmentId) => DepartmentId = departmentId;
}
```

**Steg 4: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~PersonTests"
```

**Steg 5: Commit**

```bash
git add src/TronderLeikan.Domain/Persons/ tests/TronderLeikan.Domain.Tests/Persons/
git commit -m "feat: legg til Person-entitet"
```

---

## Task 5: TournamentPointRules og Tournament-entitet

**Files:**
- Create: `src/TronderLeikan.Domain/Tournaments/TournamentPointRules.cs`
- Create: `src/TronderLeikan.Domain/Tournaments/Tournament.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Tournaments/TournamentPointRulesTests.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Tournaments/TournamentTests.cs`

**Steg 1: Skriv testene**

Opprett `tests/TronderLeikan.Domain.Tests/Tournaments/TournamentPointRulesTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class TournamentPointRulesTests
{
    [Fact]
    public void Default_ReturnererRiktigeStandardverdier()
    {
        var rules = TournamentPointRules.Default();

        rules.Participation.Should().Be(3);
        rules.FirstPlace.Should().Be(3);
        rules.SecondPlace.Should().Be(2);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(1);
        rules.OrganizedWithoutParticipation.Should().Be(3);
        rules.Spectator.Should().Be(1);
    }

    [Fact]
    public void Custom_SetsAlleVerdier()
    {
        var rules = TournamentPointRules.Custom(
            participation: 5,
            firstPlace: 5,
            secondPlace: 3,
            thirdPlace: 1,
            organizedWithParticipation: 2,
            organizedWithoutParticipation: 4,
            spectator: 0
        );

        rules.Participation.Should().Be(5);
        rules.FirstPlace.Should().Be(5);
        rules.SecondPlace.Should().Be(3);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(2);
        rules.OrganizedWithoutParticipation.Should().Be(4);
        rules.Spectator.Should().Be(0);
    }
}
```

Opprett `tests/TronderLeikan.Domain.Tests/Tournaments/TournamentTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class TournamentTests
{
    [Fact]
    public void Create_SetsNavnSlugOgId()
    {
        var tournament = Tournament.Create("Høst-leikan 2026", "host-leikan-2026");

        tournament.Id.Should().NotBeEmpty();
        tournament.Name.Should().Be("Høst-leikan 2026");
        tournament.Slug.Should().Be("host-leikan-2026");
    }

    [Fact]
    public void Create_HarStandardpoengregler()
    {
        var tournament = Tournament.Create("Test", "test");

        tournament.PointRules.Participation.Should().Be(3);
        tournament.PointRules.FirstPlace.Should().Be(3);
    }

    [Fact]
    public void UpdatePointRules_EndrerRegler()
    {
        var tournament = Tournament.Create("Test", "test");
        var nyeRegler = TournamentPointRules.Custom(5, 5, 3, 1, 2, 4, 0);

        tournament.UpdatePointRules(nyeRegler);

        tournament.PointRules.Participation.Should().Be(5);
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~TournamentPointRulesTests|FullyQualifiedName~TournamentTests"
```

**Steg 3: Implementer TournamentPointRules**

Opprett `src/TronderLeikan.Domain/Tournaments/TournamentPointRules.cs`:

```csharp
namespace TronderLeikan.Domain.Tournaments;

public sealed class TournamentPointRules
{
    private TournamentPointRules() { }

    public int Participation { get; private set; } = 3;
    public int FirstPlace { get; private set; } = 3;
    public int SecondPlace { get; private set; } = 2;
    public int ThirdPlace { get; private set; } = 1;
    public int OrganizedWithParticipation { get; private set; } = 1;
    public int OrganizedWithoutParticipation { get; private set; } = 3;
    public int Spectator { get; private set; } = 1;

    public static TournamentPointRules Default() => new();

    public static TournamentPointRules Custom(
        int participation,
        int firstPlace,
        int secondPlace,
        int thirdPlace,
        int organizedWithParticipation,
        int organizedWithoutParticipation,
        int spectator) =>
        new()
        {
            Participation = participation,
            FirstPlace = firstPlace,
            SecondPlace = secondPlace,
            ThirdPlace = thirdPlace,
            OrganizedWithParticipation = organizedWithParticipation,
            OrganizedWithoutParticipation = organizedWithoutParticipation,
            Spectator = spectator
        };
}
```

**Steg 4: Implementer Tournament**

Opprett `src/TronderLeikan.Domain/Tournaments/Tournament.cs`:

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Tournaments;

public sealed class Tournament : Entity
{
    private Tournament() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public TournamentPointRules PointRules { get; private set; } = TournamentPointRules.Default();

    public static Tournament Create(string name, string slug) =>
        new() { Id = Guid.NewGuid(), Name = name, Slug = slug };

    public void UpdatePointRules(TournamentPointRules rules) => PointRules = rules;
}
```

**Steg 5: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~TournamentPointRulesTests|FullyQualifiedName~TournamentTests"
```

**Steg 6: Commit**

```bash
git add src/TronderLeikan.Domain/Tournaments/ tests/TronderLeikan.Domain.Tests/Tournaments/
git commit -m "feat: legg til TournamentPointRules og Tournament-entitet"
```

---

## Task 6: GameType-enum og Game-entitet (grunnstruktur)

**Files:**
- Create: `src/TronderLeikan.Domain/Games/GameType.cs`
- Create: `src/TronderLeikan.Domain/Games/Game.cs`
- Create: `src/TronderLeikan.Domain/Games/Events/GameCompletedEvent.cs`
- Create: `tests/TronderLeikan.Domain.Tests/Games/GameTests.cs`

**Steg 1: Skriv testene**

Opprett `tests/TronderLeikan.Domain.Tests/Games/GameTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Tests.Games;

public class GameTests
{
    [Fact]
    public void Create_SetsNavnTournamentIdOgStandardverdier()
    {
        var tournamentId = Guid.NewGuid();

        var game = Game.Create("Kubb", tournamentId);

        game.Id.Should().NotBeEmpty();
        game.Name.Should().Be("Kubb");
        game.TournamentId.Should().Be(tournamentId);
        game.GameType.Should().Be(GameType.Standard);
        game.IsDone.Should().BeFalse();
        game.IsOrganizersParticipating.Should().BeFalse();
        game.HasBanner.Should().BeFalse();
    }

    [Fact]
    public void Create_MedGameType_SetsGameType()
    {
        var game = Game.Create("Simracing", Guid.NewGuid(), GameType.Simracing);

        game.GameType.Should().Be(GameType.Simracing);
    }

    [Fact]
    public void AddParticipant_LeggTilPerson()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddParticipant(personId);

        game.Participants.Should().ContainSingle().Which.Should().Be(personId);
    }

    [Fact]
    public void AddParticipant_DuplikatIgnoreres()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddParticipant(personId);
        game.AddParticipant(personId);

        game.Participants.Should().ContainSingle();
    }

    [Fact]
    public void AddOrganizer_LeggTilArrangør()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddOrganizer(personId, withParticipation: false);

        game.Organizers.Should().ContainSingle().Which.Should().Be(personId);
        game.IsOrganizersParticipating.Should().BeFalse();
    }

    [Fact]
    public void AddOrganizer_MedDeltakelse_SetsIsOrganizersParticipating()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.AddOrganizer(Guid.NewGuid(), withParticipation: true);

        game.IsOrganizersParticipating.Should().BeTrue();
    }

    [Fact]
    public void AddSpectator_LeggTilTilskuer()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var personId = Guid.NewGuid();

        game.AddSpectator(personId);

        game.Spectators.Should().ContainSingle().Which.Should().Be(personId);
    }

    [Fact]
    public void Complete_SetsIsDoneOgPlasseringer()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var charlie = Guid.NewGuid();

        game.Complete(
            firstPlace: [alice],
            secondPlace: [bob],
            thirdPlace: [charlie]
        );

        game.IsDone.Should().BeTrue();
        game.FirstPlace.Should().ContainSingle().Which.Should().Be(alice);
        game.SecondPlace.Should().ContainSingle().Which.Should().Be(bob);
        game.ThirdPlace.Should().ContainSingle().Which.Should().Be(charlie);
    }

    [Fact]
    public void Complete_RaiserGameCompletedEvent()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.Complete(firstPlace: [], secondPlace: [], thirdPlace: []);

        game.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GameCompletedEvent>()
            .Which.GameId.Should().Be(game.Id);
    }

    [Fact]
    public void Complete_SupportsTies()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        game.Complete(firstPlace: [alice, bob], secondPlace: [], thirdPlace: []);

        game.FirstPlace.Should().HaveCount(2).And.Contain(alice).And.Contain(bob);
    }

    [Fact]
    public void SetBanner_SetsHasBannerTilTrue()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());

        game.SetBanner();

        game.HasBanner.Should().BeTrue();
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~GameTests"
```

**Steg 3: Implementer GameType**

Opprett `src/TronderLeikan.Domain/Games/GameType.cs`:

```csharp
namespace TronderLeikan.Domain.Games;

public enum GameType
{
    Standard = 0,
    Simracing = 1,
    Bracket = 2   // reservert for fremtidig implementasjon
}
```

**Steg 4: Implementer GameCompletedEvent**

Opprett `src/TronderLeikan.Domain/Games/Events/GameCompletedEvent.cs`:

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Games.Events;

public sealed record GameCompletedEvent(Guid GameId) : IDomainEvent;
```

**Steg 5: Implementer Game**

Opprett `src/TronderLeikan.Domain/Games/Game.cs`:

```csharp
using TronderLeikan.Domain.Common;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Games;

public sealed class Game : Entity
{
    private readonly List<Guid> _participants = [];
    private readonly List<Guid> _organizers = [];
    private readonly List<Guid> _spectators = [];
    private readonly List<Guid> _firstPlace = [];
    private readonly List<Guid> _secondPlace = [];
    private readonly List<Guid> _thirdPlace = [];

    private Game() { }

    public Guid TournamentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDone { get; private set; }
    public GameType GameType { get; private set; }
    public bool IsOrganizersParticipating { get; private set; }
    public bool HasBanner { get; private set; }

    public IReadOnlyList<Guid> Participants => _participants.AsReadOnly();
    public IReadOnlyList<Guid> Organizers => _organizers.AsReadOnly();
    public IReadOnlyList<Guid> Spectators => _spectators.AsReadOnly();
    public IReadOnlyList<Guid> FirstPlace => _firstPlace.AsReadOnly();
    public IReadOnlyList<Guid> SecondPlace => _secondPlace.AsReadOnly();
    public IReadOnlyList<Guid> ThirdPlace => _thirdPlace.AsReadOnly();

    public static Game Create(string name, Guid tournamentId, GameType gameType = GameType.Standard) =>
        new() { Id = Guid.NewGuid(), Name = name, TournamentId = tournamentId, GameType = gameType };

    public void AddParticipant(Guid personId)
    {
        if (!_participants.Contains(personId))
            _participants.Add(personId);
    }

    public void AddOrganizer(Guid personId, bool withParticipation)
    {
        if (!_organizers.Contains(personId))
            _organizers.Add(personId);

        if (withParticipation)
            IsOrganizersParticipating = true;
    }

    public void AddSpectator(Guid personId)
    {
        if (!_spectators.Contains(personId))
            _spectators.Add(personId);
    }

    public void Complete(
        IEnumerable<Guid> firstPlace,
        IEnumerable<Guid> secondPlace,
        IEnumerable<Guid> thirdPlace)
    {
        _firstPlace.Clear();
        _firstPlace.AddRange(firstPlace);
        _secondPlace.Clear();
        _secondPlace.AddRange(secondPlace);
        _thirdPlace.Clear();
        _thirdPlace.AddRange(thirdPlace);

        IsDone = true;
        AddDomainEvent(new GameCompletedEvent(Id));
    }

    public void SetBanner() => HasBanner = true;
    public void RemoveBanner() => HasBanner = false;
    public void UpdateDescription(string? description) => Description = description;
}
```

**Steg 6: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~GameTests"
```

**Steg 7: Commit**

```bash
git add src/TronderLeikan.Domain/Games/ tests/TronderLeikan.Domain.Tests/Games/
git commit -m "feat: legg til GameType, Game-entitet og GameCompletedEvent"
```

---

## Task 7: SimracingResult-entitet

**Files:**
- Create: `src/TronderLeikan.Domain/Games/SimracingResult.cs`
- Create: `src/TronderLeikan.Domain/Games/Events/SimracingResultRegisteredEvent.cs`
- Modify: `tests/TronderLeikan.Domain.Tests/Games/GameTests.cs` — legg til testklasse for simracing
- Create: `tests/TronderLeikan.Domain.Tests/Games/SimracingResultTests.cs`

**Steg 1: Skriv testene**

Opprett `tests/TronderLeikan.Domain.Tests/Games/SimracingResultTests.cs`:

```csharp
using FluentAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Tests.Games;

public class SimracingResultTests
{
    [Fact]
    public void Register_SetsAlleVerdier()
    {
        var gameId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var result = SimracingResult.Register(gameId, personId, raceTimeMs: 92_543);

        result.Id.Should().NotBeEmpty();
        result.GameId.Should().Be(gameId);
        result.PersonId.Should().Be(personId);
        result.RaceTimeMs.Should().Be(92_543);
    }

    [Fact]
    public void Register_RaiserSimracingResultRegisteredEvent()
    {
        var gameId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var result = SimracingResult.Register(gameId, personId, raceTimeMs: 88_000);

        result.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SimracingResultRegisteredEvent>()
            .Which.GameId.Should().Be(gameId);
    }
}
```

**Steg 2: Kjør og bekreft FAIL**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~SimracingResultTests"
```

**Steg 3: Implementer SimracingResultRegisteredEvent**

Opprett `src/TronderLeikan.Domain/Games/Events/SimracingResultRegisteredEvent.cs`:

```csharp
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Games.Events;

public sealed record SimracingResultRegisteredEvent(Guid GameId, Guid PersonId) : IDomainEvent;
```

**Steg 4: Implementer SimracingResult**

Opprett `src/TronderLeikan.Domain/Games/SimracingResult.cs`:

```csharp
using TronderLeikan.Domain.Common;
using TronderLeikan.Domain.Games.Events;

namespace TronderLeikan.Domain.Games;

public sealed class SimracingResult : Entity
{
    private SimracingResult() { }

    public Guid GameId { get; private set; }
    public Guid PersonId { get; private set; }
    public long RaceTimeMs { get; private set; }

    public static SimracingResult Register(Guid gameId, Guid personId, long raceTimeMs)
    {
        var result = new SimracingResult
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            PersonId = personId,
            RaceTimeMs = raceTimeMs
        };

        result.AddDomainEvent(new SimracingResultRegisteredEvent(gameId, personId));

        return result;
    }
}
```

**Steg 5: Kjør og bekreft PASS**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/ --filter "FullyQualifiedName~SimracingResultTests"
```

**Steg 6: Commit**

```bash
git add src/TronderLeikan.Domain/Games/ tests/TronderLeikan.Domain.Tests/Games/
git commit -m "feat: legg til SimracingResult-entitet og SimracingResultRegisteredEvent"
```

---

## Task 8: Full testkjøring og opprydding

**Steg 1: Kjør alle domenester**

```bash
dotnet test tests/TronderLeikan.Domain.Tests/
```
Forventet: Alle tester PASS, ingen feil.

**Steg 2: Verifiser at hele solution bygger**

```bash
dotnet build TronderLeikan.slnx
```
Forventet: `Build succeeded` uten advarsler.

**Steg 3: Endelig commit**

```bash
git add .
git commit -m "feat: domenelaget fullstendig implementert med alle entiteter og tester"
```

---

## Filstruktur etter ferdigstilling

```
src/TronderLeikan.Domain/
├── Common/
│   ├── Entity.cs
│   └── IDomainEvent.cs
├── Departments/
│   └── Department.cs
├── Games/
│   ├── Events/
│   │   ├── GameCompletedEvent.cs
│   │   └── SimracingResultRegisteredEvent.cs
│   ├── Game.cs
│   ├── GameType.cs
│   └── SimracingResult.cs
├── Persons/
│   └── Person.cs
└── Tournaments/
    ├── Tournament.cs
    └── TournamentPointRules.cs

tests/TronderLeikan.Domain.Tests/
├── Common/
│   └── EntityTests.cs
├── Departments/
│   └── DepartmentTests.cs
├── Games/
│   ├── GameTests.cs
│   └── SimracingResultTests.cs
├── Persons/
│   └── PersonTests.cs
└── Tournaments/
    ├── TournamentPointRulesTests.cs
    └── TournamentTests.cs
```
