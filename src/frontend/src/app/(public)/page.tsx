import type { Metadata } from "next";
import Link from "next/link";

// Datamodell for turnering — tilsvarer API-respons fra /api/v1/tournaments
type TournamentSummaryResponse = {
  id: string;
  name: string;
  slug: string;
};

// Henter turneringer fra backend. Returnerer tomt array ved utilgjengelighet,
// slik at siden alltid rendres — selv uten API-tilkobling under utvikling.
async function getTournaments(): Promise<TournamentSummaryResponse[]> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Turneringer",
  description:
    "Oversikt over alle turneringer i TrønderLeikan. Følg med på rangeringer og resultater.",
};

// Turneringskort — lenker til turneringsdetaljsiden
function TournamentCard({ tournament }: { tournament: TournamentSummaryResponse }) {
  return (
    <Link
      href={`/tournaments/${tournament.slug}`}
      className="card group block animate-fade-up"
      style={{ textDecoration: "none" }}
    >
      {/* Aksent-stripe øverst på kortet */}
      <div
        aria-hidden="true"
        style={{
          height: "2px",
          background: `linear-gradient(90deg, var(--color-accent), transparent)`,
          marginBottom: "1.25rem",
          borderRadius: "1px",
          opacity: 0.6,
          transition: "opacity 0.2s var(--ease-out-expo)",
        }}
        className="group-hover:opacity-100"
      />

      {/* Turneringsnavn */}
      <h2
        style={{
          fontSize: "1.125rem",
          fontWeight: 700,
          letterSpacing: "-0.02em",
          color: "var(--color-text-primary)",
          marginBottom: "0.5rem",
          lineHeight: 1.3,
          transition: "color 0.2s var(--ease-out-expo)",
        }}
        className="group-hover:text-[var(--color-accent)]"
      >
        {tournament.name}
      </h2>

      {/* Lenke-indikator */}
      <span
        style={{
          fontSize: "0.8125rem",
          color: "var(--color-text-muted)",
          display: "flex",
          alignItems: "center",
          gap: "0.25rem",
          transition: "color 0.2s var(--ease-out-expo)",
        }}
        className="group-hover:text-[var(--color-accent)]"
      >
        Se resultater
        <span aria-hidden="true" style={{ transition: "transform 0.2s var(--ease-out-expo)" }} className="group-hover:translate-x-1 inline-block">→</span>
      </span>
    </Link>
  );
}

// Tom-tilstand — vises når ingen turneringer er registrert ennå
function EmptyState() {
  return (
    <div
      className="animate-fade-up"
      style={{
        gridColumn: "1 / -1",
        textAlign: "center",
        padding: "4rem 2rem",
        border: "1px dashed var(--color-border)",
        borderRadius: "0.75rem",
        color: "var(--color-text-muted)",
      }}
    >
      {/* Dekorativt ikon */}
      <div
        aria-hidden="true"
        style={{
          fontSize: "2.5rem",
          marginBottom: "1rem",
          opacity: 0.4,
        }}
      >
        ⬡
      </div>
      <p
        style={{
          fontSize: "1rem",
          fontWeight: 600,
          color: "var(--color-text-secondary)",
          marginBottom: "0.375rem",
        }}
      >
        Ingen turneringer ennå
      </p>
      <p style={{ fontSize: "0.875rem" }}>
        Turneringer vil dukke opp her når de er opprettet.
      </p>
    </div>
  );
}

// Hjem-side — Server Component som henter turneringer og viser kortgrid
export default async function HomePage() {
  const tournaments = await getTournaments();

  return (
    <div className="container section">
      {/* Sideoverskrift med aksent-dekorasjon */}
      <div
        className="animate-fade-up"
        style={{ marginBottom: "2.5rem" }}
      >
        <span className="badge-accent" style={{ marginBottom: "0.75rem" }}>
          Alle turneringer
        </span>
        <h1
          style={{
            fontSize: "clamp(1.75rem, 4vw, 2.5rem)",
            fontWeight: 800,
            letterSpacing: "-0.03em",
            color: "var(--color-text-primary)",
            lineHeight: 1.15,
            marginTop: "0.5rem",
          }}
        >
          TrønderLeikan
        </h1>
        <p
          style={{
            fontSize: "1rem",
            color: "var(--color-text-secondary)",
            marginTop: "0.5rem",
            maxWidth: "36rem",
          }}
        >
          Plattform for turneringsstyring og poengberegning i Trøndelag.
        </p>
      </div>

      {/* Kortgrid — responsivt, 1–3 kolonner */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 20rem), 1fr))",
          gap: "1rem",
        }}
      >
        {tournaments.length === 0 ? (
          <EmptyState />
        ) : (
          tournaments.map((tournament, index) => (
            <div
              key={tournament.id}
              style={{ animationDelay: `${index * 60}ms` }}
            >
              <TournamentCard tournament={tournament} />
            </div>
          ))
        )}
      </div>
    </div>
  );
}
