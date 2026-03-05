# AGENTS.md

## 🎯 Prosjektoversikt

**TrønderLeikan**: Plattform for turneringsstyring og poengberegning.

- **Backend**: .NET 10 (Clean Architecture).
- **Frontend**: Next.js 16 (`src/frontend`) kjøres med **Bun**.
- **Orkestrering**: .NET Aspire via `AppHost` (bruker `CommunityToolkit.Aspire.Hosting.Bun`).
- **Infrastruktur**: PostgreSQL (DB), Valkey (Cache).

## 🛠 Verktøy og Skills (Kommandoer)

- **Kjøring (Full Stack)**: `dotnet run --project src/TronderLeikan.AppHost`
- **Frontend-kommandoer**: Bruk `bun` i `src/frontend` (f.eks. `bun run dev` hvis kjørt manuelt).
- **Testing**: `dotnet test --filter "FullyQualifiedName~[KlasseNavn]"`
- **Helse**: Sjekk Aspire Dashboard for status på PostgreSQL, Valkey og Bun-noden.

## 🏗 Arkitektur og Flyt

- **Dependency Rule**: `API` -> `Application` -> `Domain`. `Infrastructure` implementerer interfaces fra `Application`.
- **Frontend-integrasjon**: Next.js-prosjektet i `src/frontend` er koblet til Aspire-verktøykjeden. Endringer i orkestrering gjøres i `AppHost` ved bruk av `AddBunApp`.

## 🔁 Multi-Replica og Distribuert Design

Backend kjøres alltid med **minst 2 replicas**. All backend-utvikling må ta hensyn til dette:

- **Stateless API**: Ingen in-memory state mellom requests. Session-data, caches og køer lagres eksternt (PostgreSQL, Valkey, RabbitMQ).
- **Outbox pattern for domenehendelser**: Domain events skrives til `OutboxMessages`-tabell i **samme transaksjon** som forretningsdata. En `IHostedService` leser fra outbox og publiserer til RabbitMQ. Garanterer at events aldri tapes ved krasj.
- **Idempotente event-handlere**: RabbitMQ-consumers kan motta samme melding mer enn én gang. Alle handlers må tåle dette (sjekk om allerede behandlet før handling).
- **Ingen in-process-only sideeffekter** for operasjoner som har konsekvenser på tvers av replicas — bruk RabbitMQ.
- **Distribuert lås**: Bruk PostgreSQL advisory locks eller Valkey for operasjoner som kun én replica skal utføre om gangen.

## ✅ Sjekkliste — Multi-Replica

Før ny backend-feature merges:
1. Vil dette feile hvis to replicas behandler samme request samtidig?
2. Er event-handlere idempotente?
3. Brukes outbox for domenehendelser (ikke direkte in-process dispatch)?
4. Er Aspire-konfigurasjon oppdatert med ny infrastruktur (køer, topics)?

## 🏆 Domenelogikk: Poengregler

AI må følge disse reglene strengt over generisk kunnskap:

- **Status**: Kun spill med `isDone = true` gir poeng til scoreboard.
- **Plassering**: Poeng for 1., 2. og 3. plass er **additive** (legges oppå deltakerpoeng).
- **Arrangør**: Poeng avhenger av om man også deltar (`organizedWithParticipation`).
- **Ties**: Flere personer kan dele samme plassering.

## 🔭 Observabilitet

Filosofi: **Tracing og metrics > logging.**
> "Man bruker logging for å finne ut hvorfor tracing ikke funker." — Martin Thwaits

- **Tracing**: Alle commands og queries får automatisk Activity-span via `ObservabilityBehavior` i `ISender`-pipelinen. Bruk `ActivitySource("TronderLeikan.Sender")` for egendefinerte spans i viktig forretningslogikk.
- **Metrics**: `sender.requests.total` og `sender.requests.duration` er alltid tilgjengelig og tagget med request-type og success/failure. Legg til domene-spesifikke metrics i egne behaviors ved behov.
- **Logging**: Reserver for feil som *ikke* syns i traces — oppstartssekvens, infrastrukturfeil, og «dette burde aldri skje»-tilfeller.

## ✍️ Språk og Konvensjoner

- **Kode**: Engelsk. **Kommentarer**: Norsk (viktig!).
- **Spesialtegn**: Bruk alltid æ, ø, å i norske tekster og kommentarer.
- **Modern .NET**: Bruk .NET 10 patterns (Primary constructors, implicit logging, `file`-scoped namespaces).

## ✅ Sjekkliste for ferdigstillelse

1. **Clean Architecture**: Ligger forretningslogikken i `Application` eller `Domain`?
2. **Aspire**: Er nye miljøvariabler eller tjenester lagt til i `AppHost`?
3. **Frontend**: Brukes `bun` for pakkehåndtering i `src/frontend`?
4. **Poeng**: Stemmer beregningen med de additive reglene i `docs/TRONDER_LEIKAN.md`?
5. **Review**: Inneholder PR-en en folkelig oppsummering på norsk?
