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

## 🏆 Domenelogikk: Poengregler

AI må følge disse reglene strengt over generisk kunnskap:

- **Status**: Kun spill med `isDone = true` gir poeng til scoreboard.
- **Plassering**: Poeng for 1., 2. og 3. plass er **additive** (legges oppå deltakerpoeng).
- **Arrangør**: Poeng avhenger av om man også deltar (`organizedWithParticipation`).
- **Ties**: Flere personer kan dele samme plassering.

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
