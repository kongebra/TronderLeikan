import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for turneringsdetaljer — tilsvarer API-respons fra /api/v1/tournaments/:slug
type TournamentDetailResponse = {
  id: string;
  name: string;
  slug: string;
  pointRules: {
    participantPoints: number;
    firstPlacePoints: number;
    secondPlacePoints: number;
    thirdPlacePoints: number;
    organizerWithParticipationPoints: number;
    organizerWithoutParticipationPoints: number;
    spectatorPoints: number;
  };
};

// Datamodell for scoreboard-innslag — en rad i rangeringslisten
type ScoreboardEntryResponse = {
  personId: string;
  firstName: string;
  lastName: string;
  totalPoints: number;
  rank: number;
};

// Henter turneringsdetaljer via slug. Returnerer null ved feil eller manglende ressurs.
async function getTournamentBySlug(
  slug: string
): Promise<TournamentDetailResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments/${slug}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter scoreboard for en gitt turnering. Returnerer tomt array ved feil.
async function getScoreboard(
  id: string
): Promise<ScoreboardEntryResponse[]> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments/${id}/scoreboard`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Dynamisk metadata basert på turneringsnavn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const tournament = await getTournamentBySlug(slug);

  if (!tournament) {
    return { title: "Turnering ikke funnet" };
  }

  return {
    title: tournament.name,
    description: `Poengregler, scoreboard og resultater for ${tournament.name} i TrønderLeikan.`,
  };
}

// Poengregelkort — viser en enkelt poengkategori med ikon og etikett
function PointRuleCard({
  label,
  points,
  icon,
  highlight,
}: {
  label: string;
  points: number;
  icon: string;
  highlight?: boolean;
}) {
  return (
    <div
      style={{
        backgroundColor: highlight
          ? "var(--color-accent-subtle)"
          : "var(--color-bg-elevated)",
        border: `1px solid ${highlight ? "var(--color-accent-glow)" : "var(--color-border)"}`,
        borderRadius: "0.625rem",
        padding: "1rem 1.25rem",
        display: "flex",
        alignItems: "center",
        gap: "0.875rem",
        transition: "border-color 0.2s var(--ease-out-expo)",
      }}
    >
      {/* Ikon-sirkel */}
      <div
        aria-hidden="true"
        style={{
          width: "2.25rem",
          height: "2.25rem",
          borderRadius: "50%",
          backgroundColor: highlight
            ? "var(--color-accent-glow)"
            : "var(--color-bg-overlay)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "1rem",
          flexShrink: 0,
        }}
      >
        {icon}
      </div>

      {/* Etikett og poengverdi */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div
          style={{
            fontSize: "0.8125rem",
            color: highlight
              ? "var(--color-accent)"
              : "var(--color-text-secondary)",
            fontWeight: 500,
            marginBottom: "0.125rem",
          }}
        >
          {label}
        </div>
        <div
          style={{
            fontSize: "1.25rem",
            fontWeight: 800,
            letterSpacing: "-0.03em",
            color: highlight
              ? "var(--color-accent)"
              : "var(--color-text-primary)",
            lineHeight: 1,
          }}
        >
          {points}
          <span
            style={{
              fontSize: "0.75rem",
              fontWeight: 500,
              color: highlight
                ? "var(--color-accent)"
                : "var(--color-text-muted)",
              marginLeft: "0.25rem",
              letterSpacing: "0",
            }}
          >
            poeng
          </span>
        </div>
      </div>
    </div>
  );
}

// Medalje-komponent — viser gull, sølv eller bronse for topp-3 plasseringer
function RankMedal({ rank }: { rank: number }) {
  if (rank === 1) {
    return (
      <span
        title="1. plass"
        aria-label="Gull"
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          width: "1.875rem",
          height: "1.875rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-gold-subtle)",
          border: "1px solid var(--color-gold-border)",
          color: "var(--color-gold)",
          fontSize: "0.9375rem",
          flexShrink: 0,
        }}
      >
        ⬡
      </span>
    );
  }
  if (rank === 2) {
    return (
      <span
        title="2. plass"
        aria-label="Sølv"
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          width: "1.875rem",
          height: "1.875rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-silver-subtle)",
          border: "1px solid var(--color-silver-border)",
          color: "var(--color-silver)",
          fontSize: "0.9375rem",
          flexShrink: 0,
        }}
      >
        ⬡
      </span>
    );
  }
  if (rank === 3) {
    return (
      <span
        title="3. plass"
        aria-label="Bronse"
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          width: "1.875rem",
          height: "1.875rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-bronze-subtle)",
          border: "1px solid var(--color-bronze-border)",
          color: "var(--color-bronze)",
          fontSize: "0.9375rem",
          flexShrink: 0,
        }}
      >
        ⬡
      </span>
    );
  }

  // Vanlig rangering — viser tall
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: "1.875rem",
        height: "1.875rem",
        borderRadius: "50%",
        backgroundColor: "var(--color-bg-overlay)",
        color: "var(--color-text-muted)",
        fontSize: "0.75rem",
        fontWeight: 700,
        flexShrink: 0,
      }}
    >
      {rank}
    </span>
  );
}

// Scoreboard-rad — én deltaker med rank, navn og poengsum
function ScoreboardRow({
  entry,
  index,
}: {
  entry: ScoreboardEntryResponse;
  index: number;
}) {
  const isTopThree = entry.rank <= 3;

  return (
    <div
      className="animate-fade-up"
      style={{
        animationDelay: `${index * 40}ms`,
        display: "grid",
        gridTemplateColumns: "2.5rem 1fr auto",
        alignItems: "center",
        gap: "1rem",
        padding: "0.75rem 1.25rem",
        backgroundColor: isTopThree
          ? "var(--color-bg-elevated)"
          : "transparent",
        borderRadius: isTopThree ? "0.5rem" : "0",
        borderBottom: !isTopThree
          ? "1px solid var(--color-border-subtle)"
          : "none",
        border: isTopThree ? "1px solid var(--color-border)" : undefined,
        marginBottom: isTopThree ? "0.5rem" : "0",
        transition: "background-color 0.15s var(--ease-out-expo)",
      }}
    >
      {/* Rang-indikator */}
      <div style={{ display: "flex", justifyContent: "center" }}>
        <RankMedal rank={entry.rank} />
      </div>

      {/* Spillernavn */}
      <div>
        <span
          style={{
            fontSize: "0.9375rem",
            fontWeight: isTopThree ? 700 : 500,
            color: "var(--color-text-primary)",
            letterSpacing: "-0.01em",
          }}
        >
          {entry.firstName} {entry.lastName}
        </span>
      </div>

      {/* Totalpoeng */}
      <div
        style={{
          display: "flex",
          alignItems: "baseline",
          gap: "0.25rem",
          textAlign: "right",
        }}
      >
        <span
          style={{
            fontSize: isTopThree ? "1.125rem" : "1rem",
            fontWeight: 800,
            letterSpacing: "-0.02em",
            color: isTopThree
              ? "var(--color-accent)"
              : "var(--color-text-secondary)",
          }}
        >
          {entry.totalPoints}
        </span>
        <span
          style={{
            fontSize: "0.75rem",
            color: "var(--color-text-muted)",
            fontWeight: 400,
          }}
        >
          p
        </span>
      </div>
    </div>
  );
}

// Tom scoreboard-tilstand — vises når ingen resultater finnes ennå
function EmptyScoreboard() {
  return (
    <div
      style={{
        textAlign: "center",
        padding: "3rem 2rem",
        border: "1px dashed var(--color-border)",
        borderRadius: "0.75rem",
        color: "var(--color-text-muted)",
      }}
    >
      <div
        aria-hidden="true"
        style={{ fontSize: "2rem", marginBottom: "0.75rem", opacity: 0.4 }}
      >
        ⬡
      </div>
      <p
        style={{
          fontSize: "0.9375rem",
          fontWeight: 600,
          color: "var(--color-text-secondary)",
          marginBottom: "0.25rem",
        }}
      >
        Ingen resultater ennå
      </p>
      <p style={{ fontSize: "0.875rem" }}>
        Scoreboard oppdateres når spill er fullført.
      </p>
    </div>
  );
}

// Turneringsdetalj-side — henter data server-side og rendrer poengregler og scoreboard
export default async function TournamentDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  // Hent turnering — vis 404 hvis ikke funnet
  const tournament = await getTournamentBySlug(slug);
  if (!tournament) notFound();

  // Hent scoreboard parallelt med at turnerings-data allerede er tilgjengelig
  const scoreboard = await getScoreboard(tournament.id);

  const { pointRules } = tournament;

  // Poengregler konfigurert som liste for enkel rendering
  const pointRuleItems = [
    {
      label: "Deltaker",
      points: pointRules.participantPoints,
      icon: "◈",
      highlight: false,
    },
    {
      label: "1. plass",
      points: pointRules.firstPlacePoints,
      icon: "⬡",
      highlight: true,
    },
    {
      label: "2. plass",
      points: pointRules.secondPlacePoints,
      icon: "⬡",
      highlight: false,
    },
    {
      label: "3. plass",
      points: pointRules.thirdPlacePoints,
      icon: "⬡",
      highlight: false,
    },
    {
      label: "Arrangør (med deltakelse)",
      points: pointRules.organizerWithParticipationPoints,
      icon: "◇",
      highlight: false,
    },
    {
      label: "Arrangør (uten deltakelse)",
      points: pointRules.organizerWithoutParticipationPoints,
      icon: "◇",
      highlight: false,
    },
    {
      label: "Tilskuer",
      points: pointRules.spectatorPoints,
      icon: "○",
      highlight: false,
    },
  ];

  return (
    <div className="container section">
      {/* Tilbake-lenke */}
      <div
        className="animate-fade-up"
        style={{ marginBottom: "2rem" }}
      >
        <Link
          href="/"
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
          Alle turneringer
        </Link>
      </div>

      {/* Hero-overskrift — turneringsnavn */}
      <div
        className="animate-fade-up"
        style={{ marginBottom: "3rem" }}
      >
        <span className="badge-accent" style={{ marginBottom: "0.75rem" }}>
          Turnering
        </span>
        <h1
          style={{
            fontSize: "clamp(1.75rem, 5vw, 3rem)",
            fontWeight: 800,
            letterSpacing: "-0.04em",
            color: "var(--color-text-primary)",
            lineHeight: 1.1,
            marginTop: "0.5rem",
          }}
        >
          {tournament.name}
        </h1>

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

      {/* Todelt layout — poengregler til venstre, scoreboard til høyre på bred skjerm */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 28rem), 1fr))",
          gap: "2rem",
          alignItems: "start",
        }}
      >
        {/* Poengregler-seksjon */}
        <section
          className="animate-fade-up"
          style={{ animationDelay: "60ms" }}
          aria-labelledby="point-rules-heading"
        >
          <h2
            id="point-rules-heading"
            style={{
              fontSize: "0.8125rem",
              fontWeight: 700,
              letterSpacing: "0.08em",
              textTransform: "uppercase",
              color: "var(--color-text-muted)",
              marginBottom: "1rem",
            }}
          >
            Poengregler
          </h2>

          {/* Forklaring — plasseringspoeng er additive */}
          <p
            style={{
              fontSize: "0.8125rem",
              color: "var(--color-text-muted)",
              marginBottom: "1.25rem",
              lineHeight: 1.55,
              maxWidth: "26rem",
            }}
          >
            Poeng for 1.–3. plass legges oppå deltakerpoengene. En spiller kan
            samle poeng fra flere spill i turneringen.
          </p>

          {/* Rutenett av poengregelkort */}
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 14rem), 1fr))",
              gap: "0.625rem",
            }}
          >
            {pointRuleItems.map((rule) => (
              <PointRuleCard
                key={rule.label}
                label={rule.label}
                points={rule.points}
                icon={rule.icon}
                highlight={rule.highlight}
              />
            ))}
          </div>
        </section>

        {/* Scoreboard-seksjon */}
        <section
          className="animate-fade-up"
          style={{ animationDelay: "120ms" }}
          aria-labelledby="scoreboard-heading"
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: "1rem",
              gap: "1rem",
            }}
          >
            <h2
              id="scoreboard-heading"
              style={{
                fontSize: "0.8125rem",
                fontWeight: 700,
                letterSpacing: "0.08em",
                textTransform: "uppercase",
                color: "var(--color-text-muted)",
              }}
            >
              Scoreboard
            </h2>

            {/* Antall deltakere i scoreboard */}
            {scoreboard.length > 0 && (
              <span
                style={{
                  fontSize: "0.75rem",
                  color: "var(--color-text-muted)",
                  fontWeight: 500,
                }}
              >
                {scoreboard.length} deltaker{scoreboard.length !== 1 ? "e" : ""}
              </span>
            )}
          </div>

          {/* Tabell-header — kun synlig når det finnes innslag */}
          {scoreboard.length > 0 && (
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "2.5rem 1fr auto",
                gap: "1rem",
                padding: "0 1.25rem 0.625rem",
                borderBottom: "1px solid var(--color-border)",
                marginBottom: "0.75rem",
              }}
              aria-hidden="true"
            >
              <div
                style={{
                  fontSize: "0.6875rem",
                  fontWeight: 700,
                  letterSpacing: "0.06em",
                  textTransform: "uppercase",
                  color: "var(--color-text-muted)",
                  textAlign: "center",
                }}
              >
                #
              </div>
              <div
                style={{
                  fontSize: "0.6875rem",
                  fontWeight: 700,
                  letterSpacing: "0.06em",
                  textTransform: "uppercase",
                  color: "var(--color-text-muted)",
                }}
              >
                Navn
              </div>
              <div
                style={{
                  fontSize: "0.6875rem",
                  fontWeight: 700,
                  letterSpacing: "0.06em",
                  textTransform: "uppercase",
                  color: "var(--color-text-muted)",
                }}
              >
                Poeng
              </div>
            </div>
          )}

          {/* Scoreboard-innslag eller tom-tilstand */}
          {scoreboard.length === 0 ? (
            <EmptyScoreboard />
          ) : (
            <div role="list" aria-label="Scoreboard">
              {scoreboard
                .slice()
                .sort((a, b) => a.rank - b.rank)
                .map((entry, index) => (
                  <div key={entry.personId} role="listitem">
                    <ScoreboardRow entry={entry} index={index} />
                  </div>
                ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
