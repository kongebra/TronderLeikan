# Zitadel Aspire-integrasjon — Design

## Kontekst

TrønderLeikan trenger autentisering og autorisasjon. Zitadel v4 er valgt som identity provider.
Denne filen beskriver design for lokal Aspire-integrasjon og Testcontainers-fixture for integrasjonstester.

## Arkitektur

```
Klient (nettleser / API-kall)
        │
        ▼
  Traefik (port 8080)
   ┌─────┴──────────────────┐
   │  PathPrefix /ui/v2     │  PathPrefix / (alt annet)
   ▼                        ▼
zitadel-login (3000)   zitadel-api (8080)
                             │
                             ▼
                      PostgreSQL (zitadel-db)
```

- **Traefik** er eneste eksponerte inngangspunkt (port 8080 lokalt).
- **zitadel-login** (Next.js) håndterer innloggingsflyt på `/ui/v2`.
- **zitadel-api** (Go) er Zitadel-backenden som eksponerer OIDC/GRPC-API.
- **PostgreSQL** bruker en separat database (`zitadel`) på samme Aspire-instans.

## Komponentliste og ansvar

| Komponent | Image | Ansvar |
|---|---|---|
| `zitadel-api` | `ghcr.io/zitadel/zitadel:v4.11.0` | Identity management, OIDC, GRPC |
| `zitadel-login` | `ghcr.io/zitadel/zitadel-login:v4.11.0` | Next.js UI for innlogging |
| `traefik` | `traefik:v3.6.8` | Reverse proxy, ruter til riktig backend |
| PostgreSQL | Aspire-managed | Persistent lagring for Zitadel-data |

## Env-var-mapping (docker-compose → Aspire)

| Env-var | Verdi i Aspire |
|---|---|
| `ZITADEL_EXTERNALPORT` | `8080` |
| `ZITADEL_EXTERNALSECURE` | `false` |
| `ZITADEL_DOMAIN` | `localhost` |
| `ZITADEL_DATABASE_POSTGRES_HOST` | Fra `PostgresServerResource.PrimaryEndpoint.Host` |
| `ZITADEL_DATABASE_POSTGRES_PORT` | Fra `PostgresServerResource.PrimaryEndpoint.Port` |
| `ZITADEL_DATABASE_POSTGRES_DATABASE` | `zitadel` |
| `ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_USERNAME` | Fra Aspire PostgreSQL-brukernavn |
| `ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_PASSWORD` | Fra Aspire PostgreSQL-passord |
| `ZITADEL_DATABASE_POSTGRES_USER_APPUSER_USERNAME` | `zitadel` (dedikert bruker) |
| `ZITADEL_DATABASE_POSTGRES_USER_APPUSER_PASSWORD` | `zitadel` (dedikert passord) |

## Traefik-konfigurasjon

To filer legges i `src/TronderLeikan.AppHost/traefik/`:

- `traefik.yml` — statisk konfig: entrypoint, provider, log-nivå
- `dynamic.yml` — dynamisk konfig: rutere og services med env-var-templating

Traefik v3.x støtter `{{ env "VAR" }}` i dynamisk konfig via Go-templating.

## Bootstrap-volum

Zitadel skriver PAT (Personal Access Token) for initial admin-bruker til `/app/bootstrap`.
Dette mattes til `./zitadel-bootstrap/` lokalt (relativ til AppHost working directory).
Mappen er lagt i `.gitignore` siden den inneholder hemmeligheter.

## ZitadelExtensions — Aspire API

```csharp
// Korrekt bruk av Aspire EndpointReference
.WithEnvironment("VAR", resourceBuilder.GetEndpoint("http"))
// eller i callback:
.WithEnvironment(ctx => {
    ctx.EnvironmentVariables["VAR"] = endpointRef; // EndpointReference direkte
})
```

`EndpointReference` implementerer `IValueProvider` og resolves korrekt av Aspire runtime.

## ZitadelFixture — Testcontainers

Fixture for integrasjonstester isolerer Zitadel + PostgreSQL i et dedikert Docker-nettverk:

```
Test → ZitadelFixture
         ├── NetworkBuilder → isolert nettverk
         ├── PostgreSqlContainer (alias: "postgres")
         └── ContainerBuilder → zitadel-api
                └── WaitStrategy: HTTP /debug/healthz
```

Fixture implementerer `IAsyncLifetime` (xUnit) og brukes via `[Collection(nameof(ZitadelCollection))]`.

Initial admin PAT leses fra bootstrap-filen etter oppstart for å kunne
opprette testdata via Zitadel API.

## Sikkerhetsnotater

- MasterKey skal alltid hentes fra user secrets i lokal utvikling, ikke hardkodes i kode.
- `zitadel-bootstrap/` inneholder admin PAT og skal aldri committes.
- For CI/CD: bruk environment secrets for MasterKey og bootstrapped credentials.
