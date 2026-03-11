# Frontend-design — TrønderLeikan

## Mål

Bygge et moderne, informasjonstett frontend for TrønderLeikan bestående av:
- Offentlige sider (turneringer, scoreboard, spillerprofiler)
- Adminpanel (CRUD for turneringer, spill og spillere)

## Teknologistack

- **Framework**: Next.js 16 (App Router, Server Components)
- **Styling**: Tailwind CSS v4
- **Auth**: better-auth med Zitadel OIDC-provider
- **API-klient**: Orval (generert fra OpenAPI-spec, gitignored)
- **Pakkebehandler**: Bun

## Arkitektur

### Mappestruktur

```
src/frontend/src/
├── app/
│   ├── (public)/
│   │   ├── page.tsx                          # Hjem — turneringsliste
│   │   ├── tournaments/[slug]/
│   │   │   ├── page.tsx                      # Turnering-detalj + scoreboard
│   │   │   └── games/[id]/page.tsx           # Spill-detalj
│   │   └── players/
│   │       ├── page.tsx                      # Spillerliste
│   │       └── [id]/page.tsx                 # Spillerprofil
│   ├── (admin)/
│   │   ├── layout.tsx                        # Auth-guard (session + admin-rolle)
│   │   └── admin/
│   │       ├── page.tsx                      # Dashboard
│   │       ├── tournaments/
│   │       │   ├── page.tsx                  # Liste + opprett
│   │       │   └── [id]/
│   │       │       ├── page.tsx              # Rediger turnering + poengregler
│   │       │       └── games/[gameId]/page.tsx  # Rediger spill
│   │       └── persons/page.tsx              # Liste + CRUD spillere
│   ├── api/auth/[...all]/route.ts            # better-auth handler
│   └── layout.tsx                            # Root layout
├── lib/
│   ├── auth.ts                               # better-auth server-konfig
│   ├── auth-client.ts                        # better-auth klient
│   └── api/                                  # Orval-generert (gitignored)
└── components/
    └── ui/                                   # Gjenbrukbare komponenter
```

### Data-fetching

- **Server Components** brukes for alle public sider og admin-listesider
- **Server Actions** brukes for skjema-submit i admin
- Orval-generert klient brukes i begge kontekster med `API_BASE_URL` fra miljøvariabler

## API-klientgenerering

Orval leser OpenAPI-spec fra kjørende API og genererer typer + fetch-funksjoner.

```ts
// orval.config.ts
export default {
  tronderleikan: {
    input: "http://localhost:<api-port>/swagger/v1/swagger.json",
    output: {
      target: "./src/lib/api/index.ts",
      schemas: "./src/lib/api/model",
      client: "fetch",
      baseUrl: process.env.API_BASE_URL,
    },
  },
};
```

Kjøres med `bun run generate:api` mens Aspire-stacken er oppe. Output commites ikke.

## Autentisering

### better-auth + Zitadel

```ts
// lib/auth.ts
export const auth = betterAuth({
  socialProviders: {
    oidc: {
      issuer: process.env.ZITADEL_ISSUER,
      clientId: process.env.ZITADEL_CLIENT_ID,
      clientSecret: process.env.ZITADEL_CLIENT_SECRET,
    },
  },
});
```

### Admin-guard

`(admin)/layout.tsx` henter session server-side og sjekker `admin`-rolle fra Zitadel-claims. Mangler session eller rolle → redirect til `/login` eller `/unauthorized`.

### Roller i Zitadel

`admin`-rollen opprettes i Zitadel-konsollen og tildeles brukere manuelt. Rollen medfølger som claim i OIDC-tokenet.

### Miljøvariabler (settes via Aspire AppHost)

```
ZITADEL_ISSUER
ZITADEL_CLIENT_ID
ZITADEL_CLIENT_SECRET
BETTER_AUTH_SECRET
BETTER_AUTH_URL
API_BASE_URL
```

## Sider

### Public

| Rute | Beskrivelse |
|------|-------------|
| `/` | Kortgrid med alle turneringer |
| `/tournaments/[slug]` | Turneringsinfo, spillliste, scoreboard-tabell |
| `/tournaments/[slug]/games/[id]` | Spilldetalj — plasseringer, deltakere, tilskuere |
| `/players` | Spillergrid med foto og navn |
| `/players/[id]` | Spillerprofil med turneringshistorikk |

### Admin

| Rute | Beskrivelse |
|------|-------------|
| `/admin` | Dashboard — oversikt |
| `/admin/tournaments` | Liste + opprett turnering |
| `/admin/tournaments/[id]` | Rediger turnering, poengregler, legg til spill |
| `/admin/tournaments/[id]/games/[gameId]` | Rediger spill — deltakere, plasseringer, fullfør |
| `/admin/persons` | Liste + opprett/rediger/slett spillere |

## Designretning

- Mørkt-primært tema med sterk typografi
- Scoreboard og tabeller er kjernen — tydelige hierarkier og god lesbarhet
- Plasseringsindikatorene (1./2./3.) får visuelt tyngde (gull/sølv/bronse)
- Admin-sidene er funksjonelle og rene
- Implementeres via frontend-design-skillet per side/komponent
