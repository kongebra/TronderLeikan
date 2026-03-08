import type { Metadata } from "next";
import Link from "next/link";

// Datamodell for spilleroversikt — tilsvarer API-respons fra /api/v1/persons
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// Henter alle spillere fra backend. Returnerer tomt array ved utilgjengelighet,
// slik at siden alltid rendres — selv uten API-tilkobling under utvikling.
async function getPersons(): Promise<PersonSummaryResponse[]> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Spillere",
  description:
    "Oversikt over alle spillere i TrønderLeikan. Se profiler og resultater.",
};

// Henter initialer fra for- og etternavn for avatar-fallback
function getInitials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
}

// Enkel deterministisk fargeindeks basert på navn — gir konsistent avatar-farge
function getAvatarColorIndex(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) & 0xffffffff;
  }
  return Math.abs(hash) % 5;
}

// Fargepar for initialer-avatarer — bruker CSS-variabler fra globals.css
const AVATAR_COLORS: Array<{ bg: string; text: string }> = [
  { bg: "var(--color-accent-subtle)", text: "var(--color-accent)" },
  { bg: "var(--color-silver-subtle)", text: "var(--color-silver)" },
  { bg: "var(--color-bronze-subtle)", text: "var(--color-bronze)" },
  { bg: "var(--color-bg-overlay)", text: "var(--color-text-secondary)" },
  { bg: "var(--color-gold-subtle)", text: "var(--color-gold)" },
];

// Initialer-avatar — vises når hasProfileImage er false
function InitialsAvatar({
  firstName,
  lastName,
  size = "3rem",
  fontSize = "1rem",
}: {
  firstName: string;
  lastName: string;
  size?: string;
  fontSize?: string;
}) {
  const initials = getInitials(firstName, lastName);
  const colorIndex = getAvatarColorIndex(`${firstName}${lastName}`);
  const colors = AVATAR_COLORS[colorIndex];

  return (
    <div
      aria-hidden="true"
      style={{
        width: size,
        height: size,
        borderRadius: "50%",
        backgroundColor: colors.bg,
        border: `1px solid var(--color-border)`,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize,
        fontWeight: 700,
        color: colors.text,
        letterSpacing: "-0.01em",
        flexShrink: 0,
        userSelect: "none",
      }}
    >
      {initials}
    </div>
  );
}

// Spillerkort — lenker til profil og viser profilbilde eller initialer
function PlayerCard({ person }: { person: PersonSummaryResponse }) {
  return (
    <Link
      href={`/players/${person.id}`}
      className="group animate-fade-up"
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: "0.875rem",
        padding: "1.5rem 1rem",
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.75rem",
        textDecoration: "none",
        transition:
          "border-color 0.2s var(--ease-out-expo), background-color 0.2s var(--ease-out-expo)",
      }}
    >
      {/* Profilbilde eller initialer-avatar */}
      <div
        style={{
          position: "relative",
          width: "4rem",
          height: "4rem",
          flexShrink: 0,
        }}
      >
        {person.hasProfileImage ? (
          /* eslint-disable-next-line @next/next/no-img-element */
          <img
            src={`/api/v1/persons/${person.id}/image`}
            alt={`${person.firstName} ${person.lastName}`}
            width={64}
            height={64}
            style={{
              width: "4rem",
              height: "4rem",
              borderRadius: "50%",
              objectFit: "cover",
              border: "1px solid var(--color-border)",
            }}
          />
        ) : (
          <InitialsAvatar
            firstName={person.firstName}
            lastName={person.lastName}
            size="4rem"
            fontSize="1.125rem"
          />
        )}

        {/* Subtil glans-ring ved hover */}
        <div
          aria-hidden="true"
          style={{
            position: "absolute",
            inset: "-2px",
            borderRadius: "50%",
            border: "2px solid var(--color-accent)",
            opacity: 0,
            transition: "opacity 0.2s var(--ease-out-expo)",
          }}
          className="group-hover:opacity-100"
        />
      </div>

      {/* Spillernavn */}
      <div style={{ textAlign: "center", minWidth: 0, width: "100%" }}>
        <p
          style={{
            fontSize: "0.9375rem",
            fontWeight: 700,
            letterSpacing: "-0.02em",
            color: "var(--color-text-primary)",
            lineHeight: 1.3,
            overflow: "hidden",
            textOverflow: "ellipsis",
            whiteSpace: "nowrap",
            transition: "color 0.2s var(--ease-out-expo)",
          }}
          className="group-hover:text-[var(--color-accent)]"
        >
          {person.firstName} {person.lastName}
        </p>

        {/* Lenke-indikator */}
        <span
          style={{
            fontSize: "0.75rem",
            color: "var(--color-text-muted)",
            display: "inline-flex",
            alignItems: "center",
            gap: "0.2rem",
            marginTop: "0.25rem",
            transition: "color 0.2s var(--ease-out-expo)",
          }}
          className="group-hover:text-[var(--color-accent)]"
        >
          Se profil
          <span
            aria-hidden="true"
            style={{ transition: "transform 0.2s var(--ease-out-expo)" }}
            className="group-hover:translate-x-0.5 inline-block"
          >
            →
          </span>
        </span>
      </div>
    </Link>
  );
}

// Tom-tilstand — vises når ingen spillere er registrert ennå
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
        style={{ fontSize: "2.5rem", marginBottom: "1rem", opacity: 0.4 }}
      >
        ◈
      </div>
      <p
        style={{
          fontSize: "1rem",
          fontWeight: 600,
          color: "var(--color-text-secondary)",
          marginBottom: "0.375rem",
        }}
      >
        Ingen spillere ennå
      </p>
      <p style={{ fontSize: "0.875rem" }}>
        Spillerprofiler vil dukke opp her når de er registrert.
      </p>
    </div>
  );
}

// Spillerliste-side — henter data server-side og viser responsivt kortgrid
export default async function PlayersPage() {
  const persons = await getPersons();

  // Sorter alfabetisk på etternavn, deretter fornavn
  const sorted = persons
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  return (
    <div className="container section">
      {/* Sideoverskrift med aksent-dekorasjon */}
      <div className="animate-fade-up" style={{ marginBottom: "2.5rem" }}>
        <span className="badge-accent" style={{ marginBottom: "0.75rem" }}>
          Alle spillere
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
          Spillere
        </h1>
        <p
          style={{
            fontSize: "1rem",
            color: "var(--color-text-secondary)",
            marginTop: "0.5rem",
            maxWidth: "36rem",
          }}
        >
          {sorted.length > 0
            ? `${sorted.length} spiller${sorted.length !== 1 ? "e" : ""} registrert i TrønderLeikan.`
            : "Registrerte spillere i TrønderLeikan vises her."}
        </p>
      </div>

      {/* Kortgrid — responsivt, 2–6 kolonner avhengig av skjermbredde */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns:
            "repeat(auto-fill, minmax(min(100%, 10rem), 1fr))",
          gap: "1rem",
        }}
      >
        {sorted.length === 0 ? (
          <EmptyState />
        ) : (
          sorted.map((person, index) => (
            <div
              key={person.id}
              style={{ animationDelay: `${index * 40}ms` }}
            >
              <PlayerCard person={person} />
            </div>
          ))
        )}
      </div>
    </div>
  );
}
