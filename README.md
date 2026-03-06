# Trønder Leikan

Intern plattform for turneringsstyring og poengberegning. Brukes til å administrere konkurranser, registrere resultater og vise scoreboard — alt fra gaming-turneringer til simracing.

---

## Innhold

- [Hva er dette?](#hva-er-dette)
- [Teknologistack](#teknologistack)
- [Kom i gang](#kom-i-gang)
- [Prosjektstruktur](#prosjektstruktur)
- [Arkitektur](#arkitektur)
- [API-oversikt](#api-oversikt)
- [Domenemodell og poengregler](#domenemodell-og-poengregler)
- [Konvensjoner](#konvensjoner)
- [Testing](#testing)

---

## Hva er dette?

Trønder Leikan lar administratorer:

- Opprette turneringer med egne poengregler
- Registrere spill (enkeltkonkurranser) innen en turnering
- Knytte deltakere, arrangører og tilskuere til hvert spill
- Fullføre spill med plasseringer (1., 2., 3. plass — støtter ties)
- Se live scoreboard per turnering

---

## Teknologistack

| Lag | Teknologi |
|---|---|
| Backend | .NET 10 (C#) |
| Frontend | Next.js 16, React 19, Tailwind CSS 4, TypeScript |
| Database | PostgreSQL |
| Cache | Valkey (Redis-kompatibel) |
| Identity | Zitadel v4 |\n| Orkestrering | .NET Aspire |
| Pakkehåndtering (frontend) | Bun |

---

## Kom i gang

### Forutsetninger

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL og Valkey)
- [Bun](https://bun.sh/) (for frontend)
- .NET Aspire workload: `dotnet workload install aspire`

### User secrets (én gang etter kloning)

```bash
# Postgres-passord — brukes av database og Zitadel
dotnet user-secrets --project src/TronderLeikan.AppHost \
  set "Parameters:postgres-password" "postgres-dev-local!"

# Zitadel masterkey — må være nøyaktig 32 tegn
dotnet user-secrets --project src/TronderLeikan.AppHost \
  set "Zitadel:MasterKey" "MasterkeyNeedsToHave32Chars!!!!!"
```

### Kjør hele stacken

```bash
dotnet run --project src/TronderLeikan.AppHost
```

Aspire starter opp og orkestrerer:

1. **PostgreSQL** — database
2. **DbMigrator** — kjører EF Core-migrasjoner automatisk
3. **API** — venter til migrasjoner er fullført
4. **Frontend** — Next.js via Bun

Aspire Dashboard er tilgjengelig på `https://localhost:15888` og viser logger, helse og traces for alle tjenester.

### Kjør kun frontend (manuelt)

```bash
cd src/frontend
bun install
bun run dev
```

### Kjør kun API (uten Aspire)

Sett environment-variabel `ConnectionStrings__tronderleikan` til en PostgreSQL-connection string og:

```bash
dotnet run --project src/TronderLeikan.API
```

---

## Prosjektstruktur

```
src/
├── TronderLeikan.AppHost/          # .NET Aspire — orkestrerer alle tjenester
├── TronderLeikan.ServiceDefaults/  # Delt Aspire-konfig (OpenTelemetry, helse, service discovery)
├── TronderLeikan.API/              # HTTP-lag: Controllers, Program.cs
├── TronderLeikan.Application/      # Use cases, queries, commands, Result-pattern
├── TronderLeikan.Domain/           # Entiteter, domenehendelser, forretningslogikk
├── TronderLeikan.Infrastructure/   # EF Core, PostgreSQL, migrasjoner, bildehåndtering
├── TronderLeikan.DbMigrator/       # Standalone-app som kjører migrasjoner ved oppstart
└── frontend/                       # Next.js-app med Tailwind CSS

docs/
├── TRONDER_LEIKAN.md               # Fullstendig domenedokumentasjon
└── plans/                          # Design- og implementasjonsplaner per lag
```

---

## Arkitektur

Prosjektet følger **Clean Architecture** med streng avhengighetsregel:

```
API → Application → Domain
Infrastructure implementerer interfaces fra Application
```

### Lag

**Domain** — kjerneentiteter uten eksterne avhengigheter. Ingen EF Core, ingen HTTP. Inneholder forretningslogikk og domenehendelser.

**Application** — use cases som handlers. Returnerer `Result<T>` med en sterk `Error`-type (kode + `ErrorType`-enum). Ingen domenelogikk her, kun koordinering.

**Infrastructure** — EF Core + PostgreSQL via Npgsql. Konfigurasjoner i `Persistence/Configurations/`. Domain events skrives til `EventStore`, utgående meldinger til `OutboxMessages` — begge i samme transaksjon som forretningsdata.

**API** — Controllers arver `ApiControllerBase` som tilbyr `Problem(Error)` med RFC 9457 Problem Details. Versjonert via URL-segment (`/api/v1/`).

### Multi-replica

API kjøres med minst 2 replicas. Alt state lagres eksternt:
- Ingen in-memory session-state
- Domain events via **Outbox pattern** (aldri direkte dispatch)
- Event-handlere er **idempotente**

---

## API-oversikt

Alle endepunkter er under `/api/v1/`. Feil returneres som RFC 9457 Problem Details.

```
GET    /api/v1/departments

GET    /api/v1/persons
GET    /api/v1/persons/{id}
POST   /api/v1/persons
PUT    /api/v1/persons/{id}
DELETE /api/v1/persons/{id}
PUT    /api/v1/persons/{id}/image        # multipart/form-data
DELETE /api/v1/persons/{id}/image

GET    /api/v1/tournaments
GET    /api/v1/tournaments/{slug}
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
PUT    /api/v1/games/{id}/banner         # multipart/form-data
GET    /api/v1/games/{id}/simracing-results
POST   /api/v1/games/{id}/simracing-results
POST   /api/v1/games/{id}/simracing-results/complete
```

Swagger/OpenAPI er tilgjengelig på `/openapi/v1.json` og `/swagger` i development.

---

## Domenemodell og poengregler

### Entiteter

**Tournament** — samling av spill med egne poengregler og en `slug` for URL-bruk.

**Game** — enkeltkonkurranse i en turnering. Har deltakere, arrangører og tilskuere. Støtter `GameType` (Standard, Simracing m.fl.). Kun spill med `IsDone = true` teller på scoreboard.

**Person** — person som kan delta i, arrangere eller tilskue spill.

**Department** — avdeling som personer kan tilhøre.

### Poengregler (standard)

| Handling | Poeng |
|---|---|
| Delta i spill | 3 |
| 1. plass | +3 |
| 2. plass | +2 |
| 3. plass | +1 |
| Arrangere (uten deltakelse) | 3 |
| Arrangere (med deltakelse) | +1 (i tillegg til deltaker-poeng) |
| Tilskuer | 1 |

Plasseringspoeng er **additive** — en vinner får deltakerpoeng + plasspoeng. Reglene er konfigurerbare per turnering. Ties støttes: flere kan dele samme plass.

---

## Konvensjoner

- **Kode:** engelsk
- **Kommentarer:** norsk (inkl. æ, ø, å)
- **C#:** .NET 10-mønstre — primary constructors, `file`-scoped namespaces, implicit usings
- **Frontend:** Bun som pakkebehandler — bruk aldri `npm` eller `yarn`
- **Migrasjoner:** genereres fra `Infrastructure`-prosjektet med API som startup-prosjekt

```bash
dotnet ef migrations add <Navn> \
  --project src/TronderLeikan.Infrastructure \
  --startup-project src/TronderLeikan.API
```

---

## Testing

### Kjør alle tester

```bash
dotnet test
```

### Kjør spesifikk testklasse

```bash
dotnet test --filter "FullyQualifiedName~PersonsApiTests"
```

### Akseptansetester (API)

Testprosjektet `TronderLeikan.Api.Tests` bruker `WebApplicationFactory` + `Testcontainers.PostgreSql` — ekte PostgreSQL-container, black-box mot HTTP-kontrakten. Ingen referanser til Application- eller Domain-typer i testene.

### Sjekkliste før merge

1. Ligger forretningslogikk i `Application` eller `Domain` — ikke i `API`?
2. Er nye tjenester/miljøvariabler lagt til i `AppHost`?
3. Er nye domain events håndtert via Outbox (ikke direkte dispatch)?
4. Er event-handlere idempotente?
5. Inneholder PR-beskrivelsen en folkelig oppsummering på norsk?
