import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for spillerprofil — tilsvarer API-respons fra /api/v1/persons/:id
type PersonDetailResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// Henter én spiller via ID. Returnerer null ved feil eller manglende ressurs.
async function getPersonById(
  id: string
): Promise<PersonDetailResponse | null> {
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

// Dynamisk metadata basert på spillerens navn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const person = await getPersonById(id);

  if (!person) {
    return { title: "Spiller ikke funnet" };
  }

  return {
    title: `${person.firstName} ${person.lastName}`,
    description: `Profil for ${person.firstName} ${person.lastName} i TrønderLeikan.`,
  };
}

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

// Stor initialer-avatar for profilsiden — vises når hasProfileImage er false
function InitialsAvatarLarge({
  firstName,
  lastName,
}: {
  firstName: string;
  lastName: string;
}) {
  const initials = getInitials(firstName, lastName);
  const colorIndex = getAvatarColorIndex(`${firstName}${lastName}`);
  const colors = AVATAR_COLORS[colorIndex];

  return (
    <div
      aria-hidden="true"
      style={{
        width: "7.5rem",
        height: "7.5rem",
        borderRadius: "50%",
        backgroundColor: colors.bg,
        border: "2px solid var(--color-border)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: "2.5rem",
        fontWeight: 800,
        color: colors.text,
        letterSpacing: "-0.02em",
        flexShrink: 0,
        userSelect: "none",
      }}
    >
      {initials}
    </div>
  );
}

// Spillerprofil-side — henter data server-side og viser profil med bilde eller initialer
export default async function PlayerProfilePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  // Hent spiller — vis 404 hvis ikke funnet
  const person = await getPersonById(id);
  if (!person) notFound();

  return (
    <div className="container section">
      {/* Tilbake-lenke */}
      <div className="animate-fade-up" style={{ marginBottom: "2rem" }}>
        <Link
          href="/players"
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
          Alle spillere
        </Link>
      </div>

      {/* Profilhero — avatar og navn */}
      <div
        className="animate-fade-up"
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "flex-start",
          gap: "2rem",
          marginBottom: "3rem",
        }}
      >
        {/* Avatar-blokk */}
        <div style={{ position: "relative" }}>
          {person.hasProfileImage ? (
            /* eslint-disable-next-line @next/next/no-img-element */
            <img
              src={`/api/v1/persons/${person.id}/image`}
              alt={`${person.firstName} ${person.lastName}`}
              width={120}
              height={120}
              style={{
                width: "7.5rem",
                height: "7.5rem",
                borderRadius: "50%",
                objectFit: "cover",
                border: "2px solid var(--color-border)",
              }}
            />
          ) : (
            <InitialsAvatarLarge
              firstName={person.firstName}
              lastName={person.lastName}
            />
          )}

          {/* Aksent-glød bak avatar */}
          <div
            aria-hidden="true"
            style={{
              position: "absolute",
              inset: "-4px",
              borderRadius: "50%",
              background: `radial-gradient(circle, var(--color-accent-glow) 0%, transparent 70%)`,
              zIndex: -1,
            }}
          />
        </div>

        {/* Navn og badge */}
        <div>
          <span className="badge-accent" style={{ marginBottom: "0.75rem" }}>
            Spiller
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
            {person.firstName}{" "}
            <span style={{ color: "var(--color-accent)" }}>
              {person.lastName}
            </span>
          </h1>

          {/* Dekorativ aksent-linje under tittelen */}
          <div
            aria-hidden="true"
            style={{
              marginTop: "1.25rem",
              height: "1px",
              background:
                "linear-gradient(90deg, var(--color-accent) 0%, var(--color-border) 40%, transparent 100%)",
              maxWidth: "20rem",
            }}
          />
        </div>
      </div>
    </div>
  );
}
