# Domain Design — TrønderLeikan

**Dato:** 2026-03-04
**Status:** Godkjent

---

## Kontekst

TrønderLeikan er en intern turneringsplattform. Systemet sporer spill, deltakere og poeng for én turnering per år med opptil ~100 personer og ~12 spill per turnering. Skalaen er liten og bevisst enkel.

---

## Valg og begrunnelser

| Valg | Beslutning | Begrunnelse |
|---|---|---|
| Entity-stil | Rich domain model (private setters, factory-metoder, domain events) | Best practice for Clean Architecture |
| ID-type | Vanlig `Guid` | Tilstrekkelig for denne skalaen, enklere EF Core-konfigurasjon |
| Game-modell | Separat entity med Guid ID | Gjør det enkelt å hente enkeltspill direkte |
| Aggregat-stil | Flat entity-struktur | Pragmatisk, god match mot domenet, unngår overkompleksitet |
| Bildepersistans | PostgreSQL (`bytea` i separate tabeller) | ~14 MB bilder/år — PostgreSQL håndterer dette uten problemer. Separate tabeller sikrer at bytes ikke lastes i EF Core-queries unødvendig. MinIO er overkill for denne skalaen. |
| Avdeling | Separat lookup-tabell | Brukere velger blant opprettede avdelinger, muliggjør fremtidig filtrering |
| Spilltyper | `GameType`-enum med `Simracing`-utvidelse | Alle spilltyper resolver til standard plasseringer for poengberegning |

---

## Domenemodell

### `Department` (lookup-entitet)

```
Id          Guid
Name        string (required)
```

Brukes som referanse fra `Person`. Admin oppretter og vedlikeholder avdelinger.

---

### `Person`

```
Id              Guid
FirstName       string (required)
LastName        string (required)
Department      DepartmentId (FK, nullable)
HasProfileImage bool
```

Representerer en spiller, arrangør eller tilskuer. Profilbilde lagres i `PersonImages`.

---

### `PersonImage` (separat tabell)

```
PersonId        Guid (FK, 1-til-1)
ImageData       byte[] (WebP, maks 512×512, kvadratisk)
ContentType     string
```

Servert via `GET /api/persons/{id}/image`. Bildebehandling (resize/crop/komprimering) gjøres i Infrastructure med **SixLabors.ImageSharp** før lagring.

---

### `Tournament`

```
Id          Guid
Name        string (required)
Slug        string (required, unique)
PointRules  TournamentPointRules (owned entity)
Games       IReadOnlyList<Game> (navigasjonsproperty)
```

---

### `TournamentPointRules` (owned entity på Tournament)

```
Participation                   int (default: 3)
FirstPlace                      int (default: 3)
SecondPlace                     int (default: 2)
ThirdPlace                      int (default: 1)
OrganizedWithParticipation      int (default: 1)
OrganizedWithoutParticipation   int (default: 3)
Spectator                       int (default: 1)
```

Plasseringspoeng er additive — 1. plass gir `Participation + FirstPlace`.

---

### `Game`

```
Id                          Guid
TournamentId                Guid (FK)
Name                        string (required)
Description                 string? (optional)
IsDone                      bool (default: false)
GameType                    GameType (enum)
IsOrganizersParticipating   bool (default: false)
HasBanner                   bool
```

Kun spill med `IsDone = true` inkluderes i scoreboard-beregning.

**Relasjoner (join-tabeller):**
- `GameParticipants` — PersonId × GameId
- `GameOrganizers` — PersonId × GameId
- `GameSpectators` — PersonId × GameId
- `GameFirstPlace` — PersonId × GameId
- `GameSecondPlace` — PersonId × GameId
- `GameThirdPlace` — PersonId × GameId

---

### `GameBanner` (separat tabell)

```
GameId          Guid (FK, 1-til-1)
ImageData       byte[] (WebP, maks 1920×1080)
ContentType     string
```

Servert via `GET /api/games/{id}/banner`.

---

### `GameType` (enum)

```csharp
public enum GameType
{
    Standard = 0,
    Simracing = 1,
    Bracket = 2   // reservert for fremtidig implementasjon
}
```

---

### `SimracingResult` (kun for GameType.Simracing)

```
Id          Guid
GameId      Guid (FK)
PersonId    Guid (FK)
RaceTimeMs  long  // tid i millisekunder
```

Når et simracing-spill markeres som `IsDone`, beregnes plasseringer automatisk fra `RaceTimeMs` (lavest tid = 1. plass). Plasseringene skrives til `GameFirstPlace`, `GameSecondPlace`, `GameThirdPlace` på vanlig måte — slik at poengberegningen er identisk for alle spilltyper.

---

## Domain Events

| Event | Trigger |
|---|---|
| `GameCompletedEvent` | Når `Game.Complete(...)` kalles (IsDone settes til true) |
| `SimracingResultRegisteredEvent` | Når ny tid registreres for et simracing-spill |

---

## Poengberegning (domeneregler)

For hvert spill med `IsDone = true`:

1. **Deltakelse**: `participation`-poeng til alle i `GameParticipants`
2. **Plassering**: additive poeng til `GameFirstPlace`, `GameSecondPlace`, `GameThirdPlace`
3. **Arrangør med deltakelse** (`IsOrganizersParticipating = true`): `organizedWithParticipation` + `participation`-poeng
4. **Arrangør uten deltakelse** (`IsOrganizersParticipating = false`): kun `organizedWithoutParticipation`-poeng
5. **Tilskuer**: `spectator`-poeng til alle i `GameSpectators`

Rangering: sortert på totalpoeng (synkende). Likt poeng = delt plassering.

---

## Bildeterskler og fremtidig revurdering

PostgreSQL bytea er riktig for dette prosjektet. Revurder (f.eks. MinIO/Azure Blob) når:
- Total bildedata nærmer seg **1–5 GB**
- Det er behov for **CDN/edge caching** av bilder
- Du vil ha **direkte URL-er** uten å gå via API-et

Med nåværende skala estimeres dette til å aldri bli nødvendig for denne applikasjonen.

---

## Fremtidig utvidelse: Bracket-turnering

`GameType.Bracket` er reservert. Bracket-logikk (runder, matcher, elimineringstre) bygges i en egen runde. Outputen fra en bracket-turnering vil, som alle andre spilltyper, resolve til standard plasseringer i `GameFirstPlace`/`GameSecondPlace`/`GameThirdPlace`.
