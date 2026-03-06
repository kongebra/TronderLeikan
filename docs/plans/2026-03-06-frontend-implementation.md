# Frontend Implementasjonsplan — TrønderLeikan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Mål:** Bygge komplett Next.js-frontend med public sider (turneringer, scoreboard, spillere) og adminpanel (CRUD) koblet mot eksisterende .NET API via Orval-generert klient og better-auth + Zitadel for autentisering.

**Arkitektur:** Next.js 16 App Router med Server Components for datafetching. Route groups `(public)` og `(admin)` separerer åpne og beskyttede sider. better-auth håndterer Zitadel OIDC-session. Orval genererer typer og fetch-funksjoner fra OpenAPI-spec.

**Tech Stack:** Next.js 16, React 19, Tailwind CSS v4, better-auth, Orval, Bun, .NET Aspire (CommunityToolkit.Aspire.Hosting.Bun)

---

## Task 1: Legg til frontend i Aspire AppHost

**Files:**
- Modify: `src/TronderLeikan.AppHost/AppHost.cs`
- Modify: `src/TronderLeikan.AppHost/TronderLeikan.AppHost.csproj`

**Steg 1: Legg til Bun-referanse i .csproj**

Sjekk at `CommunityToolkit.Aspire.Hosting.Bun` er i prosjektfilen:
```bash
grep -i "Bun" src/TronderLeikan.AppHost/TronderLeikan.AppHost.csproj
```
Hvis ikke — legg til:
```xml
<PackageReference Include="CommunityToolkit.Aspire.Hosting.Bun" Version="*" />
```

**Steg 2: Legg til frontend i AppHost.cs**

Legg til etter `api`-ressursen (men før `builder.Build().Run()`):
```csharp
// Frontend — Next.js via Bun
// better-auth trenger ZITADEL_ISSUER, CLIENT_ID, CLIENT_SECRET og BETTER_AUTH_SECRET
var betterAuthSecret = builder.AddParameter("better-auth-secret", secret: true);
var zitadelClientId = builder.AddParameter("zitadel-client-id", secret: false);
var zitadelClientSecret = builder.AddParameter("zitadel-client-secret", secret: true);

var frontend = builder.AddBunApp("frontend", "../frontend")
    .WithReference(api)
    .WithReference(zitadel.GetEndpoint("http"))
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("http"))
    .WithEnvironment("ZITADEL_ISSUER", "http://localhost:8080")
    .WithEnvironment("ZITADEL_CLIENT_ID", zitadelClientId)
    .WithEnvironment("ZITADEL_CLIENT_SECRET", zitadelClientSecret)
    .WithEnvironment("BETTER_AUTH_SECRET", betterAuthSecret)
    .WithEnvironment("BETTER_AUTH_URL", "http://localhost:3000")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WaitFor(api);
```

**Steg 3: Legg til user secrets for nye parametere**
```bash
cd src/TronderLeikan.AppHost
dotnet user-secrets set "Parameters:better-auth-secret" "$(openssl rand -base64 32)"
dotnet user-secrets set "Parameters:zitadel-client-id" "PLACEHOLDER"
dotnet user-secrets set "Parameters:zitadel-client-secret" "PLACEHOLDER"
```
(Client ID/secret settes etter at Zitadel-app er opprettet i Task 3)

**Steg 4: Commit**
```bash
git add src/TronderLeikan.AppHost/
git commit -m "feat(apphost): legg til frontend med Bun og better-auth miljøvariabler"
```

---

## Task 2: Installer Orval og generer API-klient

**Files:**
- Create: `src/frontend/orval.config.ts`
- Modify: `src/frontend/package.json`
- Create: `src/frontend/.gitignore` (eller modify)

**Steg 1: Installer Orval**
```bash
cd src/frontend
bun add -d orval
```

**Steg 2: Opprett orval.config.ts**
```typescript
// src/frontend/orval.config.ts
import { defineConfig } from "orval";

export default defineConfig({
  tronderleikan: {
    input: {
      target: process.env.SWAGGER_URL ?? "http://localhost:5000/swagger/v1/swagger.json",
    },
    output: {
      target: "./src/lib/api/index.ts",
      schemas: "./src/lib/api/model",
      client: "fetch",
      override: {
        mutator: {
          path: "./src/lib/api/fetcher.ts",
          name: "customFetch",
        },
      },
    },
  },
});
```

**Steg 3: Opprett custom fetcher (Server Component-kompatibel)**
```typescript
// src/frontend/src/lib/api/fetcher.ts
export async function customFetch<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  const baseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";
  const response = await fetch(`${baseUrl}${url}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
    // Deaktiver Next.js-caching for å alltid hente fersk data
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`API-feil: ${response.status} ${response.statusText}`);
  }

  // 204 No Content har ingen body
  if (response.status === 204) return undefined as T;
  return response.json();
}
```

**Steg 4: Legg til generate-script i package.json**
```json
"scripts": {
  "dev": "next dev",
  "build": "next build",
  "start": "next start",
  "lint": "eslint",
  "generate:api": "orval"
}
```

**Steg 5: Legg til generert kode i .gitignore**
Legg til i `src/frontend/.gitignore` (eller opprett den):
```
# Orval-generert API-klient
src/lib/api/index.ts
src/lib/api/model/
```

**Steg 6: Start Aspire og generer klient**
```bash
# I en terminal: start Aspire og finn API-porten i dashboard
dotnet run --project src/TronderLeikan.AppHost

# I src/frontend — kjør etter at API er oppe
SWAGGER_URL=http://localhost:<api-port>/swagger/v1/swagger.json bun run generate:api
```
Verifiser at `src/frontend/src/lib/api/` inneholder genererte filer.

**Steg 7: Commit**
```bash
git add src/frontend/orval.config.ts src/frontend/src/lib/api/fetcher.ts src/frontend/package.json src/frontend/.gitignore
git commit -m "feat(frontend): legg til Orval-konfigurasjon og API-fetcher"
```

---

## Task 3: Konfigurer better-auth med Zitadel OIDC

**Forutsetning:** Aspire-stacken må kjøre og Zitadel være tilgjengelig på `http://localhost:8080`.

**Files:**
- Create: `src/frontend/src/lib/auth.ts`
- Create: `src/frontend/src/lib/auth-client.ts`
- Create: `src/frontend/src/app/api/auth/[...all]/route.ts`
- Create: `src/frontend/src/middleware.ts`

**Steg 1: Installer better-auth**
```bash
cd src/frontend
bun add better-auth
```

**Steg 2: Opprett Zitadel OIDC-app i Zitadel-konsollen**

1. Gå til `http://localhost:8080` og logg inn med admin-brukeren
2. Opprett en ny applikasjon av typen "Web" med PKCE
3. Redirect URI: `http://localhost:3000/api/auth/callback/zitadel`
4. Post-logout redirect URI: `http://localhost:3000`
5. Kopier Client ID og Client Secret
6. Oppdater user secrets i AppHost:
```bash
cd src/TronderLeikan.AppHost
dotnet user-secrets set "Parameters:zitadel-client-id" "<client-id>"
dotnet user-secrets set "Parameters:zitadel-client-secret" "<client-secret>"
```

**Steg 3: Opprett auth.ts (server-side)**
```typescript
// src/frontend/src/lib/auth.ts
import { betterAuth } from "better-auth";

export const auth = betterAuth({
  secret: process.env.BETTER_AUTH_SECRET!,
  baseURL: process.env.BETTER_AUTH_URL ?? "http://localhost:3000",
  socialProviders: {
    oidc: {
      id: "zitadel",
      name: "Zitadel",
      issuer: process.env.ZITADEL_ISSUER!,
      clientId: process.env.ZITADEL_CLIENT_ID!,
      clientSecret: process.env.ZITADEL_CLIENT_SECRET!,
      scopes: ["openid", "profile", "email"],
    },
  },
});
```

**Steg 4: Opprett auth-client.ts (klient-side)**
```typescript
// src/frontend/src/lib/auth-client.ts
import { createAuthClient } from "better-auth/client";

export const authClient = createAuthClient({
  baseURL: process.env.NEXT_PUBLIC_BETTER_AUTH_URL ?? "http://localhost:3000",
});
```

**Steg 5: Opprett API-route for better-auth**
```typescript
// src/frontend/src/app/api/auth/[...all]/route.ts
import { auth } from "@/lib/auth";
import { toNextJsHandler } from "better-auth/next-js";

export const { GET, POST } = toNextJsHandler(auth);
```

**Steg 6: Opprett middleware for admin-beskyttelse**
```typescript
// src/frontend/src/middleware.ts
import { NextRequest, NextResponse } from "next/server";
import { auth } from "@/lib/auth";

export async function middleware(request: NextRequest) {
  // Beskytt alle /admin-ruter
  if (request.nextUrl.pathname.startsWith("/admin")) {
    const session = await auth.api.getSession({
      headers: request.headers,
    });

    if (!session) {
      return NextResponse.redirect(new URL("/login", request.url));
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/admin/:path*"],
};
```

**Steg 7: Test auth-endepunkt**
Start Aspire, gå til `http://localhost:3000/api/auth/session` — skal returnere `null` (ikke innlogget).

**Steg 8: Commit**
```bash
git add src/frontend/src/lib/auth.ts src/frontend/src/lib/auth-client.ts src/frontend/src/app/api/ src/frontend/src/middleware.ts
git commit -m "feat(frontend): legg til better-auth med Zitadel OIDC-konfigurasjon"
```

---

## Task 4: Root layout og metadata

**Files:**
- Modify: `src/frontend/src/app/layout.tsx`
- Modify: `src/frontend/src/app/globals.css`

**Steg 1: Oppdater root layout**

Bruk frontend-design-skillet (`/frontend-design`) for å designe root layout med:
- Norsk `lang`-attributt
- Riktig metadata (tittel, beskrivelse)
- Global navigasjon (nav med lenker til turneringer og spillere)
- Mørkt tema som standard
- Geist-fonten beholdes eller byttes ut av skillet

Invoke: `@frontend-design design root layout for TrønderLeikan — mørkt tema, navigasjon med Turneringer og Spillere-lenker, TrønderLeikan-logo/tittel i header`

**Steg 2: Verifiser at `bun run dev` fortsatt starter uten feil**

**Steg 3: Commit**
```bash
git add src/frontend/src/app/layout.tsx src/frontend/src/app/globals.css
git commit -m "feat(frontend): root layout med navigasjon og mørkt tema"
```

---

## Task 5: Public — Hjem (turneringsliste)

**Files:**
- Modify: `src/frontend/src/app/(public)/page.tsx`

**Steg 1: Hent turneringer server-side og design siden**

Bruk frontend-design-skillet for å implementere siden:

```typescript
// Datamodell tilgjengelig fra Orval:
// getTournaments() → TournamentSummaryResponse[]
// type TournamentSummaryResponse = { id: string; name: string; slug: string }
```

Invoke: `@frontend-design design hjem-side for TrønderLeikan — kortgrid med alle turneringer, hvert kort viser turneringsnavn og lenker til /tournaments/[slug]. Data hentes server-side med getTournaments() fra Orval-klienten.`

**Steg 2: Verifiser at siden laster og viser turneringer fra API**

**Steg 3: Commit**
```bash
git add src/frontend/src/app/\(public\)/page.tsx
git commit -m "feat(frontend): hjem-side med turneringsliste"
```

---

## Task 6: Public — Turnering-detalj og scoreboard

**Files:**
- Create: `src/frontend/src/app/(public)/tournaments/[slug]/page.tsx`

**Steg 1: Design turneringsdetaljsiden**

Tilgjengelig data:
```typescript
// getTournamentBySlug(slug) → TournamentDetailResponse
// type TournamentDetailResponse = {
//   id: string; name: string; slug: string;
//   pointRules: TournamentPointRulesResponse
// }

// getScoreboard(id) → ScoreboardEntryResponse[]
// type ScoreboardEntryResponse = {
//   personId: string; firstName: string; lastName: string;
//   totalPoints: number; rank: number
// }
```

Invoke: `@frontend-design design turneringsdetaljside for TrønderLeikan — turneringsnavn øverst, poengregler-kort (deltaker, 1./2./3. plass, arrangør, tilskuer-poeng), scoreboard-tabell med rank/navn/totalpoeng sortert etter rank. Data hentes server-side.`

**Steg 2: Legg til `generateStaticParams` for statisk pre-rendering (valgfritt)**
```typescript
export async function generateStaticParams() {
  const tournaments = await getTournaments();
  return tournaments.map((t) => ({ slug: t.slug }));
}
```

**Steg 3: Verifiser at siden laster med riktig turnering**

**Steg 4: Commit**
```bash
git add src/frontend/src/app/\(public\)/tournaments/
git commit -m "feat(frontend): turneringsdetaljside med scoreboard"
```

---

## Task 7: Public — Spill-detalj

**Files:**
- Create: `src/frontend/src/app/(public)/tournaments/[slug]/games/[id]/page.tsx`

**Steg 1: Design spilldetaljsiden**

Tilgjengelig data:
```typescript
// getGameById(id) → GameDetailResponse
// type GameDetailResponse = {
//   id: string; tournamentId: string; name: string; description?: string;
//   isDone: boolean; hasBanner: boolean; isOrganizersParticipating: boolean;
//   participants: string[]; organizers: string[]; spectators: string[];
//   firstPlace: string[]; secondPlace: string[]; thirdPlace: string[];
// }
// NB: participants/organizers/spectators er GUIDs — hent persondetaljer med getPersonById
```

Invoke: `@frontend-design design spilldetaljside for TrønderLeikan — spillnavn, beskrivelse, kort for 1./2./3. plass (gull/sølv/bronse-farge), deltakerliste, tilskuerliste, arrangørliste. Naviger tilbake til turneringen.`

**Steg 2: Verifiser visning**

**Steg 3: Commit**
```bash
git add src/frontend/src/app/\(public\)/tournaments/
git commit -m "feat(frontend): spilldetaljside med plasseringer og deltakere"
```

---

## Task 8: Public — Spillerliste og spillerprofil

**Files:**
- Create: `src/frontend/src/app/(public)/players/page.tsx`
- Create: `src/frontend/src/app/(public)/players/[id]/page.tsx`

**Steg 1: Design spillerlisten**

Tilgjengelig data:
```typescript
// getPersons() → PersonSummaryResponse[]
// type PersonSummaryResponse = {
//   id: string; firstName: string; lastName: string;
//   departmentId?: string; hasProfileImage: boolean
// }
// Profilbilde-URL: /api/v1/persons/{id}/image (hvis hasProfileImage)
```

Invoke: `@frontend-design design spillerliste for TrønderLeikan — responsivt grid (2/4/6 kolonner), hvert kort viser profilbilde (grayscale, farge på hover) og fullt navn. Sortert alfabetisk.`

**Steg 2: Design spillerprofil**

```typescript
// getPersonById(id) → PersonDetailResponse
// (se PersonDetailResponse for full struktur)
```

Invoke: `@frontend-design design spillerprofil for TrønderLeikan — navn, profilbilde, turneringshistorikk gruppert per turnering med spill og plassering.`

**Steg 3: Commit**
```bash
git add src/frontend/src/app/\(public\)/players/
git commit -m "feat(frontend): spillerliste og spillerprofil"
```

---

## Task 9: Admin — Layout med auth-guard

**Files:**
- Create: `src/frontend/src/app/(admin)/layout.tsx`
- Create: `src/frontend/src/app/(admin)/admin/page.tsx`
- Create: `src/frontend/src/app/login/page.tsx`

**Steg 1: Opprett admin-layout med server-side session-sjekk**
```typescript
// src/frontend/src/app/(admin)/layout.tsx
import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth.api.getSession({ headers: await headers() });

  if (!session) {
    redirect("/login");
  }

  return (
    <div>
      {/* Admin-navigasjon */}
      <nav>
        <a href="/admin">Dashboard</a>
        <a href="/admin/tournaments">Turneringer</a>
        <a href="/admin/persons">Spillere</a>
      </nav>
      <main>{children}</main>
    </div>
  );
}
```

**Steg 2: Opprett login-side**
```typescript
// src/frontend/src/app/login/page.tsx
"use client";
import { authClient } from "@/lib/auth-client";

export default function LoginPage() {
  return (
    <div>
      <h1>Logg inn</h1>
      <button
        onClick={() =>
          authClient.signIn.social({ provider: "zitadel", callbackURL: "/admin" })
        }
      >
        Logg inn med Zitadel
      </button>
    </div>
  );
}
```

**Steg 3: Invoke frontend-design for admin-layout og login-side**

Invoke: `@frontend-design design admin-layout og login-side for TrønderLeikan — sidebar-navigasjon for admin-seksjonen, login-side med Zitadel-knapp.`

**Steg 4: Test at `/admin` redirecter til `/login` uten session**

**Steg 5: Commit**
```bash
git add src/frontend/src/app/\(admin\)/ src/frontend/src/app/login/
git commit -m "feat(frontend): admin-layout med session-guard og login-side"
```

---

## Task 10: Admin — Spillere CRUD

**Files:**
- Create: `src/frontend/src/app/(admin)/admin/persons/page.tsx`
- Create: `src/frontend/src/app/(admin)/admin/persons/actions.ts`

**Steg 1: Opprett Server Actions for persons**
```typescript
// src/frontend/src/app/(admin)/admin/persons/actions.ts
"use server";
import { createPerson, updatePerson, deletePerson } from "@/lib/api";
import { revalidatePath } from "next/cache";

export async function createPersonAction(formData: FormData) {
  const firstName = formData.get("firstName") as string;
  const lastName = formData.get("lastName") as string;
  await createPerson({ firstName, lastName });
  revalidatePath("/admin/persons");
}

export async function deletePersonAction(id: string) {
  await deletePerson(id);
  revalidatePath("/admin/persons");
}
```

**Steg 2: Design person-admin-siden**

Invoke: `@frontend-design design admin-side for spillere i TrønderLeikan — tabell med alle spillere (navn, avdeling, bilde), opprett-skjema (fornavn, etternavn), slett-knapp per rad. Bruker Server Actions.`

**Steg 3: Verifiser at opprette og slette spillere fungerer**

**Steg 4: Commit**
```bash
git add src/frontend/src/app/\(admin\)/admin/persons/
git commit -m "feat(frontend): admin-side for spillere med CRUD"
```

---

## Task 11: Admin — Turneringer og spill

**Files:**
- Create: `src/frontend/src/app/(admin)/admin/tournaments/page.tsx`
- Create: `src/frontend/src/app/(admin)/admin/tournaments/actions.ts`
- Create: `src/frontend/src/app/(admin)/admin/tournaments/[id]/page.tsx`
- Create: `src/frontend/src/app/(admin)/admin/tournaments/[id]/games/[gameId]/page.tsx`
- Create: `src/frontend/src/app/(admin)/admin/tournaments/[id]/games/[gameId]/actions.ts`

**Steg 1: Opprett Server Actions for turneringer**
```typescript
// src/frontend/src/app/(admin)/admin/tournaments/actions.ts
"use server";
import { createTournament } from "@/lib/api";
import { revalidatePath } from "next/cache";

export async function createTournamentAction(formData: FormData) {
  const name = formData.get("name") as string;
  const slug = name.toLowerCase().replace(/\s+/g, "-");
  await createTournament({ name, slug });
  revalidatePath("/admin/tournaments");
}
```

**Steg 2: Opprett Server Actions for spill**
```typescript
// src/frontend/src/app/(admin)/admin/tournaments/[id]/games/[gameId]/actions.ts
"use server";
import {
  addParticipant, addOrganizer, addSpectator,
  completeGame, createGame
} from "@/lib/api";
import { revalidatePath } from "next/cache";

export async function addParticipantAction(gameId: string, personId: string) {
  await addParticipant(gameId, { personId });
  revalidatePath(`/admin/tournaments`);
}

export async function completeGameAction(gameId: string, formData: FormData) {
  const firstPlace = formData.getAll("firstPlace") as string[];
  const secondPlace = formData.getAll("secondPlace") as string[];
  const thirdPlace = formData.getAll("thirdPlace") as string[];
  await completeGame(gameId, { firstPlace, secondPlace, thirdPlace });
  revalidatePath(`/admin/tournaments`);
}
```

**Steg 3: Design turneringsliste-admin**

Invoke: `@frontend-design design admin-side for turneringer — tabell med navn og slug, opprett-skjema, lenker til turneringsdetalj.`

**Steg 4: Design turneringsdetalj-admin (spill + poengregler)**

Invoke: `@frontend-design design admin-turnering-detaljside — liste over spill med status (ferdig/pågående), opprett-spill-knapp, poengregler-skjema med alle felt.`

**Steg 5: Design spillredigering-admin**

Invoke: `@frontend-design design admin-spill-side — legg til deltakere, arrangører, tilskuere (søk/velg spiller), fullfør-spill med plasseringer (multiselect for 1./2./3. plass).`

**Steg 6: Commit**
```bash
git add src/frontend/src/app/\(admin\)/admin/tournaments/
git commit -m "feat(frontend): admin-sider for turneringer og spill"
```

---

## Task 12: Push og verifiser

**Steg 1: Sjekk at alt kompilerer**
```bash
cd src/frontend
bun run build
```
Ingen TypeScript-feil.

**Steg 2: Kjør full stack**
```bash
dotnet run --project src/TronderLeikan.AppHost
```
Verifiser i Aspire Dashboard at `frontend`-ressursen starter og er `Running`.

**Steg 3: Manuell smoke-test**
- [ ] `/` — turneringsliste vises
- [ ] `/tournaments/[slug]` — turneringsdetalj og scoreboard
- [ ] `/players` — spillergrid
- [ ] `/admin` uten innlogging → redirect til `/login`
- [ ] `/login` → Zitadel-innlogging fungerer
- [ ] `/admin` etter innlogging → dashboard vises

**Steg 4: Push og opprett PR**
```bash
git push origin <branch-navn>
gh pr create --title "feat: Next.js frontend med public sider og adminpanel"
```
