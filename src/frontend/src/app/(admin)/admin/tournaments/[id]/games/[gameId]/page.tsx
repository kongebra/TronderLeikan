import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { addParticipantAction, completeGameAction } from "./actions";

// Datamodell for spill med detaljer — inkluderer deltakere og plasseringer
type GameDetailResponse = {
  id: string;
  tournamentId: string;
  name: string;
  description?: string;
  isDone: boolean;
  gameType: string;
  isOrganizersParticipating: boolean;
  participants: string[];
  organizers: string[];
  spectators: string[];
  firstPlace: string[];
  secondPlace: string[];
  thirdPlace: string[];
};

// Datamodell for spilleroversikt — brukes for å slå opp navn på person-ID
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  hasProfileImage: boolean;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter ett spill fra backend — returnerer null ved feil
async function getGame(gameId: string): Promise<GameDetailResponse | null> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/games/${gameId}`, {
      cache: "no-store",
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter alle spillere fra backend — returnerer tom liste ved feil
async function getPersons(): Promise<PersonSummaryResponse[]> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/persons`, {
      cache: "no-store",
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Next.js 16 async params — params er et Promise
type Props = {
  params: Promise<{ id: string; gameId: string }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  // Avventer async params før vi kan hente spillnavnet
  const { gameId } = await params;
  const game = await getGame(gameId);
  return {
    title: game?.name ?? "Spill",
  };
}

// Admin-detaljside for ett spill — deltakere, arrangører, tilskuere og fullføring
export default async function AdminGameDetailPage({ params }: Props) {
  // Avventer async params — Next.js 16-krav
  const { id: tournamentId, gameId } = await params;

  // Henter spill og spillere parallelt for å unngå sekvensielle nettverksforespørsler
  const [game, persons] = await Promise.all([getGame(gameId), getPersons()]);

  // Returnerer 404 dersom spillet ikke finnes
  if (!game) {
    notFound();
  }

  // Hjelpefunksjon — slår opp fullt navn på en person via ID
  function getPersonName(personId: string): string {
    const person = persons.find((p) => p.id === personId);
    if (!person) return personId.slice(0, 8) + "…";
    return `${person.firstName} ${person.lastName}`;
  }

  // Spillere som ikke allerede er deltakere i dette spillet — kandidater for å legge til
  const availablePersons = persons.filter(
    (p) => !game.participants.includes(p.id)
  );

  // Sorterer tilgjengelige spillere alfabetisk på etternavn
  const sortedAvailable = availablePersons
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  // Bundne Server Actions — binder gameId og tournamentId inn i actionene
  const addParticipant = addParticipantAction.bind(null);
  const completeGame = completeGameAction.bind(null, gameId, tournamentId);

  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN SPILL DETALJ — deltakere, arrangører og fullføring
           ============================================================ */

        /* Tilbake-lenke */
        .back-link {
          display: inline-flex;
          align-items: center;
          gap: 0.375rem;
          font-size: 0.8125rem;
          font-weight: 500;
          color: var(--color-text-muted);
          text-decoration: none;
          margin-bottom: 1.5rem;
          transition: color 0.18s var(--ease-out-expo);
        }

        .back-link:hover {
          color: var(--color-accent);
        }

        /* Sideoverskrift */
        .game-header {
          margin-bottom: 2rem;
        }

        .game-eyebrow {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
          margin-bottom: 0.375rem;
          display: flex;
          align-items: center;
          gap: 0.5rem;
        }

        .game-title {
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.1;
          margin: 0 0 0.5rem;
        }

        .game-meta {
          display: flex;
          align-items: center;
          gap: 0.75rem;
          flex-wrap: wrap;
        }

        /* Badge for spilltype */
        .game-type-badge {
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.1875rem 0.5rem;
          border-radius: 9999px;
          background-color: var(--color-bg-overlay);
          color: var(--color-text-muted);
          border: 1px solid var(--color-border-subtle);
        }

        /* Badge for ferdig-status */
        .game-done-badge {
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.1875rem 0.5rem;
          border-radius: 9999px;
          background-color: var(--color-success-subtle);
          color: var(--color-success);
          border: 1px solid var(--color-success-border);
        }

        /* Badge for pågående-status */
        .game-active-badge {
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.1875rem 0.5rem;
          border-radius: 9999px;
          background-color: var(--color-accent-subtle);
          color: var(--color-accent);
          border: 1px solid var(--color-accent-glow);
        }

        /* Aksent-skillelinje */
        .section-divider {
          width: 3rem;
          height: 2px;
          background: linear-gradient(
            90deg,
            var(--color-accent) 0%,
            transparent 100%
          );
          border: none;
          margin: 1.25rem 0 2rem;
        }

        /* ---- Panel — felles stil ---- */

        .admin-panel {
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          padding: 1.5rem;
          margin-bottom: 1.5rem;
        }

        .admin-panel-title {
          font-size: 0.875rem;
          font-weight: 700;
          letter-spacing: -0.01em;
          color: var(--color-text-primary);
          margin: 0 0 1.25rem;
          display: flex;
          align-items: center;
          gap: 0.5rem;
        }

        .admin-panel-title::before {
          content: "";
          display: inline-block;
          width: 0.25rem;
          height: 1em;
          background-color: var(--color-accent);
          border-radius: 2px;
        }

        /* ---- Legg til deltaker ---- */

        .add-participant-form {
          display: flex;
          flex-wrap: wrap;
          gap: 0.75rem;
          align-items: flex-end;
        }

        .form-field {
          display: flex;
          flex-direction: column;
          gap: 0.375rem;
          flex: 1 1 220px;
        }

        .form-label {
          font-size: 0.75rem;
          font-weight: 600;
          letter-spacing: 0.04em;
          text-transform: uppercase;
          color: var(--color-text-muted);
        }

        /* Nedtrekksmeny */
        .form-select {
          height: 2.375rem;
          padding: 0 0.75rem;
          background-color: var(--color-bg-base);
          border: 1px solid var(--color-border);
          border-radius: 0.5rem;
          color: var(--color-text-primary);
          font-size: 0.875rem;
          font-family: var(--font-sans);
          outline: none;
          cursor: pointer;
          transition:
            border-color 0.18s var(--ease-out-expo),
            box-shadow 0.18s var(--ease-out-expo);
          width: 100%;
          appearance: none;
          background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12' fill='none'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%234d5268' stroke-width='1.25' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
          background-repeat: no-repeat;
          background-position: right 0.75rem center;
          padding-right: 2.25rem;
        }

        .form-select:focus {
          border-color: var(--color-accent);
          box-shadow: 0 0 0 3px var(--color-accent-glow);
        }

        /* Primærknapp */
        .btn-primary {
          height: 2.375rem;
          padding: 0 1.125rem;
          background-color: var(--color-accent);
          color: var(--color-on-accent);
          font-size: 0.875rem;
          font-weight: 700;
          font-family: var(--font-sans);
          letter-spacing: -0.01em;
          border: none;
          border-radius: 0.5rem;
          cursor: pointer;
          flex-shrink: 0;
          align-self: flex-end;
          transition:
            opacity 0.18s var(--ease-out-expo),
            transform 0.18s var(--ease-out-expo);
          display: inline-flex;
          align-items: center;
          gap: 0.375rem;
        }

        .btn-primary:hover {
          opacity: 0.88;
          transform: translateY(-1px);
        }

        .btn-primary:active {
          transform: translateY(0);
        }

        /* Deaktivert knapp */
        .btn-primary:disabled {
          opacity: 0.4;
          cursor: not-allowed;
          transform: none;
        }

        /* ---- Personlister (deltakere, arrangører, tilskuere) ---- */

        .persons-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
          gap: 1rem;
          margin-bottom: 1.5rem;
        }

        .persons-group {
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          overflow: hidden;
        }

        .persons-group-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 0.875rem 1rem;
          border-bottom: 1px solid var(--color-border-subtle);
        }

        .persons-group-title {
          font-size: 0.8125rem;
          font-weight: 700;
          letter-spacing: -0.01em;
          color: var(--color-text-primary);
          margin: 0;
        }

        .persons-group-count {
          font-size: 0.6875rem;
          font-weight: 600;
          padding: 0.1rem 0.4rem;
          border-radius: 9999px;
          background-color: var(--color-accent-subtle);
          color: var(--color-accent);
          border: 1px solid var(--color-accent-glow);
        }

        .persons-list {
          list-style: none;
          margin: 0;
          padding: 0;
        }

        .persons-list-item {
          display: flex;
          align-items: center;
          gap: 0.625rem;
          padding: 0.625rem 1rem;
          border-bottom: 1px solid var(--color-border-subtle);
          font-size: 0.875rem;
          color: var(--color-text-secondary);
        }

        .persons-list-item:last-child {
          border-bottom: none;
        }

        /* Liten avatar-sirkkel med initialer */
        .person-avatar {
          width: 1.75rem;
          height: 1.75rem;
          border-radius: 50%;
          background-color: var(--color-accent-subtle);
          border: 1px solid var(--color-accent-glow);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 0.625rem;
          font-weight: 700;
          color: var(--color-accent);
          flex-shrink: 0;
        }

        .person-name {
          font-weight: 500;
          color: var(--color-text-primary);
        }

        /* Tom-tilstand for personlister */
        .persons-empty {
          padding: 1.25rem 1rem;
          font-size: 0.8125rem;
          color: var(--color-text-muted);
          text-align: center;
        }

        /* ---- Fullfør spill — plasserings-checkboxer ---- */

        .complete-game-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
          gap: 1.25rem;
          margin-bottom: 1.25rem;
        }

        .placement-section {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;
        }

        .placement-label {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.06em;
          text-transform: uppercase;
          display: flex;
          align-items: center;
          gap: 0.375rem;
        }

        /* Farger for plaseringsoverskrifter */
        .placement-label--gold   { color: var(--color-gold); }
        .placement-label--silver { color: var(--color-silver); }
        .placement-label--bronze { color: var(--color-bronze); }

        .placement-options {
          display: flex;
          flex-direction: column;
          gap: 0.25rem;
        }

        /* Avkryssingsboks-rad */
        .checkbox-row {
          display: flex;
          align-items: center;
          gap: 0.5rem;
          padding: 0.375rem 0.625rem;
          border-radius: 0.4rem;
          cursor: pointer;
          transition: background-color 0.15s var(--ease-out-expo);
        }

        .checkbox-row:hover {
          background-color: var(--color-bg-overlay);
        }

        .checkbox-input {
          width: 1rem;
          height: 1rem;
          accent-color: var(--color-accent);
          cursor: pointer;
          flex-shrink: 0;
        }

        .checkbox-name {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
          cursor: pointer;
        }

        /* Advarsel for ferdig spill */
        .game-done-notice {
          display: flex;
          align-items: flex-start;
          gap: 0.625rem;
          padding: 0.875rem 1rem;
          background-color: var(--color-success-subtle);
          border: 1px solid var(--color-success-border);
          border-radius: 0.5rem;
          margin-bottom: 1rem;
          font-size: 0.875rem;
          color: var(--color-success);
        }

        /* Plasseringsresultat-visning (for fullførte spill) */
        .placement-result {
          display: flex;
          flex-direction: column;
          gap: 0.625rem;
        }

        .placement-result-row {
          display: flex;
          align-items: baseline;
          gap: 0.75rem;
        }

        .placement-medal {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.04em;
          min-width: 5.5rem;
        }

        .placement-medal--gold   { color: var(--color-gold); }
        .placement-medal--silver { color: var(--color-silver); }
        .placement-medal--bronze { color: var(--color-bronze); }

        .placement-names {
          font-size: 0.875rem;
          color: var(--color-text-primary);
          font-weight: 500;
        }

        /* Ingen deltakere-merknad */
        .no-participants-notice {
          padding: 1rem;
          background-color: var(--color-bg-overlay);
          border: 1px solid var(--color-border-subtle);
          border-radius: 0.5rem;
          font-size: 0.875rem;
          color: var(--color-text-muted);
          text-align: center;
        }
      `}</style>

      {/* Tilbake-lenke til turnerings-detalj */}
      <Link
        href={`/admin/tournaments/${tournamentId}`}
        className="back-link"
      >
        {/* Pil-venstre-ikon */}
        <svg
          width="14"
          height="14"
          viewBox="0 0 14 14"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
        >
          <path
            d="M11 7H3M3 7L6.5 3.5M3 7L6.5 10.5"
            stroke="currentColor"
            strokeWidth="1.25"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        Tilbake til turnering
      </Link>

      {/* Sideoverskrift */}
      <header className="game-header">
        <p className="game-eyebrow">
          Spill
          {game.isOrganizersParticipating && (
            <span style={{ color: "var(--color-text-muted)", fontWeight: 400 }}>
              · Arrangørene deltar
            </span>
          )}
        </p>
        <h1 className="game-title">{game.name}</h1>
        <div className="game-meta">
          <span className="game-type-badge">{game.gameType}</span>
          {game.isDone ? (
            <span className="game-done-badge">Ferdig</span>
          ) : (
            <span className="game-active-badge">Pågår</span>
          )}
        </div>
      </header>

      <hr className="section-divider" />

      {/* ---- Legg til deltaker ---- */}
      {!game.isDone && (
        <section className="admin-panel" aria-labelledby="add-participant-title">
          <h2 className="admin-panel-title" id="add-participant-title">
            Legg til deltaker
          </h2>

          {sortedAvailable.length === 0 ? (
            <p className="no-participants-notice">
              Alle registrerte spillere er allerede lagt til som deltakere.
            </p>
          ) : (
            /* Skjema bruker inline Server Action — binder inn gameId og tournamentId */
            <form
              action={async (formData: FormData) => {
                "use server";
                const personId = formData.get("personId") as string;
                if (personId) {
                  await addParticipantAction(gameId, personId, tournamentId);
                }
              }}
              className="add-participant-form"
            >
              <div className="form-field">
                <label htmlFor="personId" className="form-label">
                  Spiller
                </label>
                <select
                  id="personId"
                  name="personId"
                  className="form-select"
                  required
                >
                  <option value="">Velg spiller…</option>
                  {sortedAvailable.map((person) => (
                    <option key={person.id} value={person.id}>
                      {person.lastName}, {person.firstName}
                    </option>
                  ))}
                </select>
              </div>

              <button type="submit" className="btn-primary">
                {/* Pluss-ikon */}
                <svg
                  width="14"
                  height="14"
                  viewBox="0 0 14 14"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                  aria-hidden="true"
                >
                  <path
                    d="M7 2V12M2 7H12"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                  />
                </svg>
                Legg til
              </button>
            </form>
          )}
        </section>
      )}

      {/* ---- Personoversikt — deltakere, arrangører, tilskuere ---- */}
      <div className="persons-grid">
        {/* Deltakere */}
        <div className="persons-group">
          <div className="persons-group-header">
            <h3 className="persons-group-title">Deltakere</h3>
            <span className="persons-group-count">
              {game.participants.length}
            </span>
          </div>
          {game.participants.length === 0 ? (
            <p className="persons-empty">Ingen deltakere ennå</p>
          ) : (
            <ul className="persons-list">
              {game.participants.map((personId) => {
                const name = getPersonName(personId);
                // Initialer fra fullt navn — opp til 2 tegn
                const initials = name
                  .split(" ")
                  .filter(Boolean)
                  .slice(0, 2)
                  .map((w) => w[0]?.toUpperCase() ?? "")
                  .join("");
                return (
                  <li key={personId} className="persons-list-item">
                    <div className="person-avatar" aria-hidden="true">
                      {initials}
                    </div>
                    <span className="person-name">{name}</span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        {/* Arrangører */}
        <div className="persons-group">
          <div className="persons-group-header">
            <h3 className="persons-group-title">Arrangører</h3>
            <span className="persons-group-count">
              {game.organizers.length}
            </span>
          </div>
          {game.organizers.length === 0 ? (
            <p className="persons-empty">Ingen arrangører registrert</p>
          ) : (
            <ul className="persons-list">
              {game.organizers.map((personId) => {
                const name = getPersonName(personId);
                const initials = name
                  .split(" ")
                  .filter(Boolean)
                  .slice(0, 2)
                  .map((w) => w[0]?.toUpperCase() ?? "")
                  .join("");
                return (
                  <li key={personId} className="persons-list-item">
                    <div className="person-avatar" aria-hidden="true">
                      {initials}
                    </div>
                    <span className="person-name">{name}</span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        {/* Tilskuere */}
        <div className="persons-group">
          <div className="persons-group-header">
            <h3 className="persons-group-title">Tilskuere</h3>
            <span className="persons-group-count">
              {game.spectators.length}
            </span>
          </div>
          {game.spectators.length === 0 ? (
            <p className="persons-empty">Ingen tilskuere registrert</p>
          ) : (
            <ul className="persons-list">
              {game.spectators.map((personId) => {
                const name = getPersonName(personId);
                const initials = name
                  .split(" ")
                  .filter(Boolean)
                  .slice(0, 2)
                  .map((w) => w[0]?.toUpperCase() ?? "")
                  .join("");
                return (
                  <li key={personId} className="persons-list-item">
                    <div className="person-avatar" aria-hidden="true">
                      {initials}
                    </div>
                    <span className="person-name">{name}</span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </div>

      {/* ---- Plasseringer / Fullfør spill ---- */}
      <section className="admin-panel" aria-labelledby="placement-title">
        <h2 className="admin-panel-title" id="placement-title">
          {game.isDone ? "Plasseringer" : "Fullfør spill"}
        </h2>

        {game.isDone ? (
          <>
            {/* Spillet er allerede fullført — viser registrerte plasseringer */}
            <div className="game-done-notice" role="status">
              {/* Hake-ikon */}
              <svg
                width="16"
                height="16"
                viewBox="0 0 16 16"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                aria-hidden="true"
                style={{ flexShrink: 0, marginTop: "1px" }}
              >
                <path
                  d="M3 8L6.5 11.5L13 5"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
              Dette spillet er fullført og gir poeng til scoreboard.
            </div>

            <div className="placement-result">
              <div className="placement-result-row">
                <span className="placement-medal placement-medal--gold">
                  🥇 1. plass
                </span>
                <span className="placement-names">
                  {game.firstPlace.length > 0
                    ? game.firstPlace.map(getPersonName).join(", ")
                    : "—"}
                </span>
              </div>
              <div className="placement-result-row">
                <span className="placement-medal placement-medal--silver">
                  🥈 2. plass
                </span>
                <span className="placement-names">
                  {game.secondPlace.length > 0
                    ? game.secondPlace.map(getPersonName).join(", ")
                    : "—"}
                </span>
              </div>
              <div className="placement-result-row">
                <span className="placement-medal placement-medal--bronze">
                  🥉 3. plass
                </span>
                <span className="placement-names">
                  {game.thirdPlace.length > 0
                    ? game.thirdPlace.map(getPersonName).join(", ")
                    : "—"}
                </span>
              </div>
            </div>
          </>
        ) : game.participants.length === 0 ? (
          /* Ingen deltakere ennå — kan ikke fullføre */
          <p className="no-participants-notice">
            Legg til minst én deltaker før spillet kan fullføres.
          </p>
        ) : (
          /*
            Skjema for å fullføre spillet — multiselect via checkboxer per plassering.
            Plasseringspoeng er additive og legges oppå deltakerpoeng (jf. domenelogikk).
            Flere spillere kan dele samme plassering (ties er tillatt).
          */
          <form action={completeGame}>
            <div className="complete-game-grid">
              {/* 1. plass */}
              <div className="placement-section">
                <p className="placement-label placement-label--gold">
                  {/* Gull-sirkel */}
                  <svg
                    width="12"
                    height="12"
                    viewBox="0 0 12 12"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    aria-hidden="true"
                  >
                    <circle
                      cx="6"
                      cy="6"
                      r="5"
                      stroke="currentColor"
                      strokeWidth="1.25"
                    />
                  </svg>
                  1. plass
                </p>
                <div className="placement-options">
                  {game.participants.map((personId) => (
                    <label key={personId} className="checkbox-row">
                      <input
                        type="checkbox"
                        name="firstPlace"
                        value={personId}
                        className="checkbox-input"
                      />
                      <span className="checkbox-name">
                        {getPersonName(personId)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {/* 2. plass */}
              <div className="placement-section">
                <p className="placement-label placement-label--silver">
                  <svg
                    width="12"
                    height="12"
                    viewBox="0 0 12 12"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    aria-hidden="true"
                  >
                    <circle
                      cx="6"
                      cy="6"
                      r="5"
                      stroke="currentColor"
                      strokeWidth="1.25"
                    />
                  </svg>
                  2. plass
                </p>
                <div className="placement-options">
                  {game.participants.map((personId) => (
                    <label key={personId} className="checkbox-row">
                      <input
                        type="checkbox"
                        name="secondPlace"
                        value={personId}
                        className="checkbox-input"
                      />
                      <span className="checkbox-name">
                        {getPersonName(personId)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {/* 3. plass */}
              <div className="placement-section">
                <p className="placement-label placement-label--bronze">
                  <svg
                    width="12"
                    height="12"
                    viewBox="0 0 12 12"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    aria-hidden="true"
                  >
                    <circle
                      cx="6"
                      cy="6"
                      r="5"
                      stroke="currentColor"
                      strokeWidth="1.25"
                    />
                  </svg>
                  3. plass
                </p>
                <div className="placement-options">
                  {game.participants.map((personId) => (
                    <label key={personId} className="checkbox-row">
                      <input
                        type="checkbox"
                        name="thirdPlace"
                        value={personId}
                        className="checkbox-input"
                      />
                      <span className="checkbox-name">
                        {getPersonName(personId)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            </div>

            <button type="submit" className="btn-primary">
              {/* Hake-ikon */}
              <svg
                width="14"
                height="14"
                viewBox="0 0 14 14"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                aria-hidden="true"
              >
                <path
                  d="M2.5 7L5.5 10L11.5 4"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
              Fullfør spill
            </button>
          </form>
        )}
      </section>
    </>
  );
}
