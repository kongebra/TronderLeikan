import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for spilldetaljer — tilsvarer API-respons fra /api/v1/games/:id
type GameDetailResponse = {
  id: string;
  tournamentId: string;
  name: string;
  description?: string;
  isDone: boolean;
  gameType: string;
  hasBanner: boolean;
  isOrganizersParticipating: boolean;
  participants: string[];  // GUIDs
  organizers: string[];    // GUIDs
  spectators: string[];    // GUIDs
  firstPlace: string[];    // GUIDs
  secondPlace: string[];   // GUIDs
  thirdPlace: string[];    // GUIDs
};

// Datamodell for personsammendrag — brukes til å slå opp navn fra GUID
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  hasProfileImage: boolean;
};

// Henter spilldetaljer via ID. Returnerer null ved feil eller manglende ressurs.
async function getGame(id: string): Promise<GameDetailResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/games/${id}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter personsammendrag via ID. Returnerer null ved ukjent GUID eller feil.
async function getPerson(id: string): Promise<PersonSummaryResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons/${id}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter navn for en liste med GUIDs parallelt og filtrerer bort nullverdier
async function resolvePersons(ids: string[]): Promise<PersonSummaryResponse[]> {
  const results = await Promise.all(ids.map((id) => getPerson(id)));
  return results.filter((p): p is PersonSummaryResponse => p !== null);
}

// Dynamisk metadata basert på spillnavn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const game = await getGame(id);

  if (!game) {
    return { title: "Spill ikke funnet" };
  }

  return {
    title: game.name,
    description: game.description ?? `Spilldetaljer for ${game.name} i TrønderLeikan.`,
  };
}

// Statusbadge — viser om spillet er ferdig eller pågår
function StatusBadge({ isDone }: { isDone: boolean }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: "0.375rem",
        padding: "0.1875rem 0.625rem",
        borderRadius: "9999px",
        fontSize: "0.75rem",
        fontWeight: 600,
        letterSpacing: "0.04em",
        backgroundColor: isDone
          ? "rgba(61, 158, 110, 0.13)"
          : "var(--color-accent-subtle)",
        color: isDone ? "var(--color-success)" : "var(--color-accent)",
        border: `1px solid ${isDone ? "rgba(61, 158, 110, 0.3)" : "var(--color-accent-glow)"}`,
      }}
    >
      {/* Statusindikator-prikk */}
      <span
        aria-hidden="true"
        style={{
          width: "0.4375rem",
          height: "0.4375rem",
          borderRadius: "50%",
          backgroundColor: isDone ? "var(--color-success)" : "var(--color-accent)",
          flexShrink: 0,
        }}
      />
      {isDone ? "Ferdig" : "Pågår"}
    </span>
  );
}

// Spilltypebadge — viser spillkategorien
function GameTypeBadge({ gameType }: { gameType: string }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "0.1875rem 0.625rem",
        borderRadius: "9999px",
        fontSize: "0.75rem",
        fontWeight: 600,
        letterSpacing: "0.04em",
        backgroundColor: "var(--color-bg-overlay)",
        color: "var(--color-text-secondary)",
        border: "1px solid var(--color-border)",
      }}
    >
      {gameType}
    </span>
  );
}

// Plasseringsikon — gull, sølv eller bronse med riktige CSS-variabler
function PlacementIcon({ place }: { place: 1 | 2 | 3 }) {
  const configs = {
    1: {
      label: "Gull — 1. plass",
      color: "var(--color-gold)",
      bg: "var(--color-gold-subtle)",
      border: "var(--color-gold-border)",
      symbol: "⬡",
    },
    2: {
      label: "Sølv — 2. plass",
      color: "var(--color-silver)",
      bg: "var(--color-silver-subtle)",
      border: "var(--color-silver-border)",
      symbol: "⬡",
    },
    3: {
      label: "Bronse — 3. plass",
      color: "var(--color-bronze)",
      bg: "var(--color-bronze-subtle)",
      border: "var(--color-bronze-border)",
      symbol: "⬡",
    },
  } as const;

  const cfg = configs[place];

  return (
    <span
      aria-label={cfg.label}
      title={cfg.label}
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: "2.25rem",
        height: "2.25rem",
        borderRadius: "50%",
        backgroundColor: cfg.bg,
        border: `1px solid ${cfg.border}`,
        color: cfg.color,
        fontSize: "1rem",
        flexShrink: 0,
      }}
    >
      {cfg.symbol}
    </span>
  );
}

// Én plasseringslinje med ikon og deltakernavn
function PlacementRow({
  place,
  persons,
}: {
  place: 1 | 2 | 3;
  persons: PersonSummaryResponse[];
}) {
  const placeLabels: Record<1 | 2 | 3, string> = {
    1: "1. plass",
    2: "2. plass",
    3: "3. plass",
  };

  if (persons.length === 0) return null;

  return (
    <div
      style={{
        display: "flex",
        alignItems: "flex-start",
        gap: "1rem",
        padding: "0.875rem 1.25rem",
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.625rem",
      }}
    >
      <PlacementIcon place={place} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div
          style={{
            fontSize: "0.6875rem",
            fontWeight: 700,
            letterSpacing: "0.07em",
            textTransform: "uppercase",
            color: "var(--color-text-muted)",
            marginBottom: "0.25rem",
          }}
        >
          {placeLabels[place]}
        </div>
        {/* Viser alle som deler plassen (ties) på separate linjer */}
        {persons.map((p) => (
          <div
            key={p.id}
            style={{
              fontSize: "1rem",
              fontWeight: 700,
              letterSpacing: "-0.015em",
              color: "var(--color-text-primary)",
              lineHeight: 1.35,
            }}
          >
            {p.firstName} {p.lastName}
          </div>
        ))}
      </div>
    </div>
  );
}

// Personliste — viser en gruppe deltakere, arrangører eller tilskuere
function PersonList({
  persons,
  emptyText,
}: {
  persons: PersonSummaryResponse[];
  emptyText: string;
}) {
  if (persons.length === 0) {
    return (
      <p
        style={{
          fontSize: "0.875rem",
          color: "var(--color-text-muted)",
          fontStyle: "italic",
          padding: "0.75rem 0",
        }}
      >
        {emptyText}
      </p>
    );
  }

  return (
    <ul
      style={{
        listStyle: "none",
        margin: 0,
        padding: 0,
        display: "flex",
        flexDirection: "column",
        gap: "0.375rem",
      }}
    >
      {persons.map((p, index) => (
        <li
          key={p.id}
          className="animate-fade-up"
          style={{
            animationDelay: `${index * 30}ms`,
            display: "flex",
            alignItems: "center",
            gap: "0.625rem",
            padding: "0.5rem 0.75rem",
            borderRadius: "0.375rem",
            backgroundColor: "var(--color-bg-elevated)",
            border: "1px solid var(--color-border-subtle)",
            fontSize: "0.9rem",
            fontWeight: 500,
            color: "var(--color-text-primary)",
          }}
        >
          {/* Initialer-avatar */}
          <span
            aria-hidden="true"
            style={{
              width: "1.75rem",
              height: "1.75rem",
              borderRadius: "50%",
              backgroundColor: "var(--color-bg-overlay)",
              border: "1px solid var(--color-border)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: "0.6875rem",
              fontWeight: 700,
              color: "var(--color-text-muted)",
              flexShrink: 0,
              letterSpacing: "0.02em",
            }}
          >
            {p.firstName[0]}{p.lastName[0]}
          </span>
          {p.firstName} {p.lastName}
        </li>
      ))}
    </ul>
  );
}

// Seksjonstittel — gjenbrukbar overskrift for seksjoner
function SectionHeading({
  id,
  children,
  count,
}: {
  id: string;
  children: React.ReactNode;
  count?: number;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "0.625rem",
        marginBottom: "0.875rem",
      }}
    >
      <h2
        id={id}
        style={{
          fontSize: "0.8125rem",
          fontWeight: 700,
          letterSpacing: "0.08em",
          textTransform: "uppercase",
          color: "var(--color-text-muted)",
        }}
      >
        {children}
      </h2>
      {/* Antall-pill */}
      {count !== undefined && (
        <span
          style={{
            fontSize: "0.6875rem",
            fontWeight: 600,
            color: "var(--color-text-muted)",
            backgroundColor: "var(--color-bg-overlay)",
            border: "1px solid var(--color-border-subtle)",
            borderRadius: "9999px",
            padding: "0.0625rem 0.4375rem",
          }}
        >
          {count}
        </span>
      )}
    </div>
  );
}

// Spilldetalj-side — henter data server-side og rendrer plasseringer og deltakere
export default async function GamePage({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}) {
  const { slug, id } = await params;

  // Hent spilldata — vis 404 hvis ikke funnet
  const game = await getGame(id);
  if (!game) notFound();

  // Hent navn for alle GUIDs parallelt — plasseringer og deltakerlister
  const [
    firstPlacePersons,
    secondPlacePersons,
    thirdPlacePersons,
    participantPersons,
    organizerPersons,
    spectatorPersons,
  ] = await Promise.all([
    resolvePersons(game.firstPlace),
    resolvePersons(game.secondPlace),
    resolvePersons(game.thirdPlace),
    resolvePersons(game.participants),
    resolvePersons(game.organizers),
    resolvePersons(game.spectators),
  ]);

  // Avgjør om plasseringsseksjonen skal vises
  const hasResults =
    game.isDone &&
    (firstPlacePersons.length > 0 ||
      secondPlacePersons.length > 0 ||
      thirdPlacePersons.length > 0);

  return (
    <div className="container section">
      {/* Tilbake-lenke til turneringen */}
      <div className="animate-fade-up" style={{ marginBottom: "2rem" }}>
        <Link
          href={`/tournaments/${slug}`}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "0.375rem",
            fontSize: "0.8125rem",
            color: "var(--color-text-muted)",
            textDecoration: "none",
            transition: "color 0.2s var(--ease-out-expo)",
          }}
          className="hover:text-[var(--color-text-secondary)]"
        >
          <span aria-hidden="true">←</span>
          Tilbake til turneringen
        </Link>
      </div>

      {/* Hero-overskrift — spillnavn med badges */}
      <div className="animate-fade-up" style={{ marginBottom: "3rem" }}>
        {/* Badges for spilltype og status */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            flexWrap: "wrap",
            gap: "0.5rem",
            marginBottom: "0.875rem",
          }}
        >
          <GameTypeBadge gameType={game.gameType} />
          <StatusBadge isDone={game.isDone} />
          {/* Arrangørdeltagelsesbadge — kun synlig hvis relevant */}
          {game.isOrganizersParticipating && (
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                padding: "0.1875rem 0.625rem",
                borderRadius: "9999px",
                fontSize: "0.75rem",
                fontWeight: 600,
                letterSpacing: "0.04em",
                backgroundColor: "rgba(58, 123, 213, 0.13)",
                color: "var(--color-info)",
                border: "1px solid rgba(58, 123, 213, 0.27)",
              }}
            >
              Arrangør deltar
            </span>
          )}
        </div>

        <h1
          style={{
            fontSize: "clamp(1.75rem, 5vw, 3rem)",
            fontWeight: 800,
            letterSpacing: "-0.04em",
            color: "var(--color-text-primary)",
            lineHeight: 1.1,
            marginBottom: game.description ? "1rem" : "0",
          }}
        >
          {game.name}
        </h1>

        {/* Valgfri beskrivelse */}
        {game.description && (
          <p
            style={{
              fontSize: "1rem",
              color: "var(--color-text-secondary)",
              lineHeight: 1.65,
              maxWidth: "44rem",
              marginTop: "0.625rem",
            }}
          >
            {game.description}
          </p>
        )}

        {/* Dekorativ aksent-linje under tittelen */}
        <div
          aria-hidden="true"
          style={{
            marginTop: "1.25rem",
            height: "1px",
            background:
              "linear-gradient(90deg, var(--color-accent) 0%, var(--color-border) 40%, transparent 100%)",
            maxWidth: "24rem",
          }}
        />
      </div>

      {/* Todelt layout — plasseringer til venstre, deltakerlister til høyre */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 28rem), 1fr))",
          gap: "2.5rem",
          alignItems: "start",
        }}
      >
        {/* Plasseringsseksjon — kun synlig når spillet er ferdig og har resultater */}
        {game.isDone ? (
          <section
            className="animate-fade-up"
            style={{ animationDelay: "60ms" }}
            aria-labelledby="placements-heading"
          >
            <SectionHeading id="placements-heading">Plasseringer</SectionHeading>

            {hasResults ? (
              <div style={{ display: "flex", flexDirection: "column", gap: "0.625rem" }}>
                <PlacementRow place={1} persons={firstPlacePersons} />
                <PlacementRow place={2} persons={secondPlacePersons} />
                <PlacementRow place={3} persons={thirdPlacePersons} />
              </div>
            ) : (
              /* Tomt plasseringsresultat — spillet er ferdig men ingen registrert */
              <div
                style={{
                  textAlign: "center",
                  padding: "2.5rem 1.5rem",
                  border: "1px dashed var(--color-border)",
                  borderRadius: "0.75rem",
                  color: "var(--color-text-muted)",
                }}
              >
                <div
                  aria-hidden="true"
                  style={{ fontSize: "1.75rem", marginBottom: "0.625rem", opacity: 0.35 }}
                >
                  ⬡
                </div>
                <p
                  style={{
                    fontSize: "0.9rem",
                    fontWeight: 600,
                    color: "var(--color-text-secondary)",
                    marginBottom: "0.25rem",
                  }}
                >
                  Ingen plasseringer registrert
                </p>
                <p style={{ fontSize: "0.8125rem" }}>
                  Resultater er ikke lagt inn for dette spillet ennå.
                </p>
              </div>
            )}
          </section>
        ) : (
          /* Spillet pågår — vis info-kort i stedet for plasseringer */
          <section
            className="animate-fade-up"
            style={{ animationDelay: "60ms" }}
            aria-labelledby="status-heading"
          >
            <SectionHeading id="status-heading">Status</SectionHeading>
            <div
              style={{
                padding: "1.5rem",
                backgroundColor: "var(--color-accent-subtle)",
                border: "1px solid var(--color-accent-glow)",
                borderRadius: "0.75rem",
              }}
            >
              <p
                style={{
                  fontSize: "0.9375rem",
                  color: "var(--color-accent)",
                  fontWeight: 600,
                  marginBottom: "0.25rem",
                }}
              >
                Spillet pågår
              </p>
              <p
                style={{
                  fontSize: "0.8125rem",
                  color: "var(--color-text-secondary)",
                  lineHeight: 1.55,
                }}
              >
                Plasseringer og poeng registreres når spillet er fullført.
              </p>
            </div>
          </section>
        )}

        {/* Deltakerlister — deltakere, arrangører og tilskuere */}
        <div
          className="animate-fade-up"
          style={{ animationDelay: "120ms", display: "flex", flexDirection: "column", gap: "2rem" }}
        >
          {/* Deltakere */}
          <section aria-labelledby="participants-heading">
            <SectionHeading id="participants-heading" count={participantPersons.length}>
              Deltakere
            </SectionHeading>
            <PersonList
              persons={participantPersons}
              emptyText="Ingen deltakere registrert."
            />
          </section>

          {/* Arrangører */}
          <section aria-labelledby="organizers-heading">
            <SectionHeading id="organizers-heading" count={organizerPersons.length}>
              Arrangører
            </SectionHeading>
            <PersonList
              persons={organizerPersons}
              emptyText="Ingen arrangører registrert."
            />
          </section>

          {/* Tilskuere — vises kun hvis det finnes tilskuere */}
          {spectatorPersons.length > 0 && (
            <section aria-labelledby="spectators-heading">
              <SectionHeading id="spectators-heading" count={spectatorPersons.length}>
                Tilskuere
              </SectionHeading>
              <PersonList
                persons={spectatorPersons}
                emptyText="Ingen tilskuere registrert."
              />
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
