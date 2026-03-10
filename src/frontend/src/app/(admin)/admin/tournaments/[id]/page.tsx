import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { updatePointRulesAction, createGameAction } from "./actions";

// Datamodell for turnering med detaljer — inkluderer poengregler
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

// Datamodell for spill — brukes i spilliste
type GameSummaryResponse = {
  id: string;
  tournamentId: string;
  name: string;
  isDone: boolean;
  gameType: string;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter turnering med detaljer fra backend — returnerer null ved feil
async function getTournament(
  id: string
): Promise<TournamentDetailResponse | null> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/tournaments/${id}`, {
      cache: "no-store",
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter alle spill for en turnering — returnerer tom liste ved feil
async function getGames(tournamentId: string): Promise<GameSummaryResponse[]> {
  try {
    const res = await fetch(
      `${API_BASE}/api/v1/tournaments/${tournamentId}/games`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Next.js 16 async params — params er et Promise
type Props = {
  params: Promise<{ id: string }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  // Avventer async params før vi kan hente turneringsnavnet
  const { id } = await params;
  const tournament = await getTournament(id);
  return {
    title: tournament?.name ?? "Turnering",
  };
}

// Admin-detaljside for en enkelt turnering — poengregler og spilliste
export default async function AdminTournamentDetailPage({ params }: Props) {
  // Avventer async params — Next.js 16-krav
  const { id } = await params;

  // Henter turnering og spill parallelt for å unngå sekvensielle nettverksforespørsler
  const [tournament, games] = await Promise.all([
    getTournament(id),
    getGames(id),
  ]);

  // Returnerer 404 dersom turneringen ikke finnes
  if (!tournament) {
    notFound();
  }

  // Hjelpefunksjon — genererer bundet Server Action for poengregler
  const updateRules = updatePointRulesAction.bind(null, tournament.id);
  // Hjelpefunksjon — genererer bundet Server Action for opprett spill
  const createGame = createGameAction.bind(null, tournament.id);

  // Sorterer spill: uferdige øverst, deretter alfabetisk
  const sortedGames = games
    .slice()
    .sort((a, b) => {
      if (a.isDone !== b.isDone) return a.isDone ? 1 : -1;
      return a.name.localeCompare(b.name, "nb");
    });

  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN TURNERING DETALJ — poengregler, spilliste og skjemaer
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

        .tournament-header {
          margin-bottom: 2rem;
        }

        .tournament-eyebrow {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
          margin-bottom: 0.375rem;
        }

        .tournament-title {
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.1;
          margin: 0 0 0.375rem;
        }

        .tournament-slug {
          font-size: 0.8125rem;
          font-family: "Courier New", Courier, monospace;
          color: var(--color-text-muted);
          margin: 0;
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

        /* ---- Poengregler-skjema ---- */

        /* Rutenett for poengregler — 2 kolonner */
        .point-rules-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
          gap: 1rem;
          margin-bottom: 1.25rem;
        }

        .form-field {
          display: flex;
          flex-direction: column;
          gap: 0.375rem;
        }

        .form-label {
          font-size: 0.75rem;
          font-weight: 600;
          letter-spacing: 0.04em;
          text-transform: uppercase;
          color: var(--color-text-muted);
        }

        /* Tallinngangsfelt */
        .form-input-number {
          height: 2.375rem;
          padding: 0 0.75rem;
          background-color: var(--color-bg-base);
          border: 1px solid var(--color-border);
          border-radius: 0.5rem;
          color: var(--color-text-primary);
          font-size: 0.875rem;
          font-family: var(--font-sans);
          outline: none;
          transition:
            border-color 0.18s var(--ease-out-expo),
            box-shadow 0.18s var(--ease-out-expo);
          width: 100%;
        }

        .form-input-number:focus {
          border-color: var(--color-accent);
          box-shadow: 0 0 0 3px var(--color-accent-glow);
        }

        /* Tekstinngangsfelt */
        .form-input {
          height: 2.375rem;
          padding: 0 0.75rem;
          background-color: var(--color-bg-base);
          border: 1px solid var(--color-border);
          border-radius: 0.5rem;
          color: var(--color-text-primary);
          font-size: 0.875rem;
          font-family: var(--font-sans);
          outline: none;
          transition:
            border-color 0.18s var(--ease-out-expo),
            box-shadow 0.18s var(--ease-out-expo);
          width: 100%;
        }

        .form-input::placeholder {
          color: var(--color-text-muted);
        }

        .form-input:focus {
          border-color: var(--color-accent);
          box-shadow: 0 0 0 3px var(--color-accent-glow);
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

        /* Avkrysningsboks-rad */
        .form-checkbox-row {
          display: flex;
          align-items: center;
          gap: 0.5rem;
          padding: 0.625rem 0;
        }

        .form-checkbox {
          width: 1rem;
          height: 1rem;
          accent-color: var(--color-accent);
          cursor: pointer;
          flex-shrink: 0;
        }

        .form-checkbox-label {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
          cursor: pointer;
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

        /* ---- Opprett-spill-skjema ---- */

        .create-game-form {
          display: flex;
          flex-wrap: wrap;
          gap: 0.75rem;
          align-items: flex-end;
        }

        .create-game-field {
          display: flex;
          flex-direction: column;
          gap: 0.375rem;
          flex: 1 1 180px;
        }

        /* ---- Spilliste ---- */

        .games-list {
          display: flex;
          flex-direction: column;
          gap: 0;
        }

        /* Spillrad */
        .game-row {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 1rem;
          padding: 0.875rem 1.25rem;
          border-bottom: 1px solid var(--color-border-subtle);
          transition: background-color 0.15s var(--ease-out-expo);
        }

        .game-row:last-child {
          border-bottom: none;
        }

        .game-row:hover {
          background-color: var(--color-bg-overlay);
        }

        .game-row-left {
          display: flex;
          align-items: center;
          gap: 0.875rem;
          min-width: 0;
        }

        /* Statusindikator — grønn for ferdig, gul for aktiv */
        .game-status-dot {
          width: 0.5rem;
          height: 0.5rem;
          border-radius: 50%;
          flex-shrink: 0;
        }

        .game-status-dot--done {
          background-color: var(--color-success);
        }

        .game-status-dot--active {
          background-color: var(--color-accent);
        }

        .game-name {
          font-size: 0.9375rem;
          font-weight: 600;
          letter-spacing: -0.01em;
          color: var(--color-text-primary);
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .game-type-badge {
          font-size: 0.6875rem;
          font-weight: 600;
          padding: 0.125rem 0.45rem;
          border-radius: 9999px;
          background-color: var(--color-bg-overlay);
          color: var(--color-text-muted);
          border: 1px solid var(--color-border-subtle);
          white-space: nowrap;
          flex-shrink: 0;
        }

        /* Ferdig-badge */
        .game-done-badge {
          font-size: 0.6875rem;
          font-weight: 600;
          padding: 0.125rem 0.45rem;
          border-radius: 9999px;
          background-color: var(--color-success-subtle);
          color: var(--color-success);
          border: 1px solid var(--color-success-border);
          white-space: nowrap;
          flex-shrink: 0;
        }

        /* Åpne-lenke for spill */
        .btn-open-game {
          height: 2rem;
          padding: 0 0.75rem;
          background: transparent;
          border: 1px solid var(--color-border-subtle);
          border-radius: 0.4rem;
          color: var(--color-text-muted);
          font-size: 0.8125rem;
          font-weight: 500;
          font-family: var(--font-sans);
          cursor: pointer;
          text-decoration: none;
          transition:
            color 0.18s var(--ease-out-expo),
            background-color 0.18s var(--ease-out-expo),
            border-color 0.18s var(--ease-out-expo);
          display: inline-flex;
          align-items: center;
          gap: 0.35rem;
          flex-shrink: 0;
        }

        .btn-open-game:hover {
          color: var(--color-accent);
          background-color: var(--color-accent-subtle);
          border-color: var(--color-accent-glow);
        }

        /* Tabelloverskrift */
        .table-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 1rem 1.25rem;
          border-bottom: 1px solid var(--color-border-subtle);
        }

        .table-header-title {
          font-size: 0.875rem;
          font-weight: 700;
          color: var(--color-text-primary);
          letter-spacing: -0.01em;
          margin: 0;
        }

        .table-count-badge {
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.125rem 0.5rem;
          border-radius: 9999px;
          background-color: var(--color-accent-subtle);
          color: var(--color-accent);
          border: 1px solid var(--color-accent-glow);
        }

        /* Tom-tilstand */
        .empty-state {
          padding: 3rem 2rem;
          text-align: center;
          color: var(--color-text-muted);
        }

        .empty-state-icon {
          font-size: 1.75rem;
          margin-bottom: 0.75rem;
          opacity: 0.35;
        }

        .empty-state-title {
          font-size: 0.9375rem;
          font-weight: 600;
          color: var(--color-text-secondary);
          margin: 0 0 0.25rem;
        }

        .empty-state-desc {
          font-size: 0.875rem;
          margin: 0;
        }

        /* ---- Seksjonsoverskrift ---- */

        .section-heading {
          font-size: 1.125rem;
          font-weight: 700;
          letter-spacing: -0.03em;
          color: var(--color-text-primary);
          margin: 0 0 1rem;
        }

        .games-section {
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          overflow: hidden;
          margin-bottom: 1.5rem;
        }
      `}</style>

      {/* Tilbake-lenke til turneringsliste */}
      <Link href="/admin/tournaments" className="back-link">
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
        Alle turneringer
      </Link>

      {/* Sideoverskrift */}
      <header className="tournament-header">
        <p className="tournament-eyebrow">Turnering</p>
        <h1 className="tournament-title">{tournament.name}</h1>
        <p className="tournament-slug">/{tournament.slug}</p>
      </header>

      <hr className="section-divider" />

      {/* ---- Poengregler ---- */}
      <section className="admin-panel" aria-labelledby="point-rules-title">
        <h2 className="admin-panel-title" id="point-rules-title">
          Poengregler
        </h2>

        {/*
          Poengregler er additive: plasseringspoeng legges oppå deltakerpoeng.
          Se domenelogikk i CLAUDE.md for detaljer om beregningsregler.
        */}
        <form action={updateRules}>
          <div className="point-rules-grid">
            <div className="form-field">
              <label htmlFor="participantPoints" className="form-label">
                Deltaker
              </label>
              <input
                id="participantPoints"
                name="participantPoints"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.participantPoints}
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label htmlFor="firstPlacePoints" className="form-label">
                1. plass (additivt)
              </label>
              <input
                id="firstPlacePoints"
                name="firstPlacePoints"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.firstPlacePoints}
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label htmlFor="secondPlacePoints" className="form-label">
                2. plass (additivt)
              </label>
              <input
                id="secondPlacePoints"
                name="secondPlacePoints"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.secondPlacePoints}
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label htmlFor="thirdPlacePoints" className="form-label">
                3. plass (additivt)
              </label>
              <input
                id="thirdPlacePoints"
                name="thirdPlacePoints"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.thirdPlacePoints}
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label
                htmlFor="organizerWithParticipationPoints"
                className="form-label"
              >
                Arrangør m/ deltakelse
              </label>
              <input
                id="organizerWithParticipationPoints"
                name="organizerWithParticipationPoints"
                type="number"
                min="0"
                defaultValue={
                  tournament.pointRules.organizerWithParticipationPoints
                }
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label
                htmlFor="organizerWithoutParticipationPoints"
                className="form-label"
              >
                Arrangør u/ deltakelse
              </label>
              <input
                id="organizerWithoutParticipationPoints"
                name="organizerWithoutParticipationPoints"
                type="number"
                min="0"
                defaultValue={
                  tournament.pointRules.organizerWithoutParticipationPoints
                }
                required
                className="form-input-number"
              />
            </div>

            <div className="form-field">
              <label htmlFor="spectatorPoints" className="form-label">
                Tilskuer
              </label>
              <input
                id="spectatorPoints"
                name="spectatorPoints"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.spectatorPoints}
                required
                className="form-input-number"
              />
            </div>
          </div>

          <button type="submit" className="btn-primary">
            {/* Lagre-ikon */}
            <svg
              width="14"
              height="14"
              viewBox="0 0 14 14"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
              aria-hidden="true"
            >
              <path
                d="M2 12H12M7 2V9M7 9L4.5 6.5M7 9L9.5 6.5"
                stroke="currentColor"
                strokeWidth="1.25"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            Lagre poengregler
          </button>
        </form>
      </section>

      {/* ---- Opprett spill ---- */}
      <section className="admin-panel" aria-labelledby="create-game-title">
        <h2 className="admin-panel-title" id="create-game-title">
          Legg til nytt spill
        </h2>

        <form action={createGame} className="create-game-form">
          <div className="create-game-field">
            <label htmlFor="gameName" className="form-label">
              Navn
            </label>
            <input
              id="gameName"
              name="name"
              type="text"
              required
              placeholder="Bowling"
              className="form-input"
            />
          </div>

          <div className="create-game-field">
            <label htmlFor="gameType" className="form-label">
              Spilltype
            </label>
            <select id="gameType" name="gameType" className="form-select">
              <option value="Standard">Standard</option>
              <option value="Simracing">Simracing</option>
            </select>
          </div>

          <div
            className="create-game-field"
            style={{ justifyContent: "flex-end" }}
          >
            <div className="form-checkbox-row">
              <input
                id="isOrganizersParticipating"
                name="isOrganizersParticipating"
                type="checkbox"
                className="form-checkbox"
              />
              <label
                htmlFor="isOrganizersParticipating"
                className="form-checkbox-label"
              >
                Arrangørene deltar
              </label>
            </div>
          </div>

          <button
            type="submit"
            className="btn-primary"
            style={{ alignSelf: "flex-end" }}
          >
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
            Opprett spill
          </button>
        </form>
      </section>

      {/* ---- Spilliste ---- */}
      <section
        className="games-section"
        aria-labelledby="games-list-title"
      >
        {/* Listeoverskrift med teller */}
        <div className="table-header">
          <h2 className="table-header-title" id="games-list-title">
            Spill i denne turneringen
          </h2>
          {sortedGames.length > 0 && (
            <span className="table-count-badge" aria-live="polite">
              {sortedGames.length}{" "}
              {sortedGames.length === 1 ? "spill" : "spill"}
            </span>
          )}
        </div>

        {sortedGames.length === 0 ? (
          /* Tom-tilstand — ingen spill ennå */
          <div className="empty-state">
            <div className="empty-state-icon" aria-hidden="true">
              ◇
            </div>
            <p className="empty-state-title">Ingen spill ennå</p>
            <p className="empty-state-desc">
              Bruk skjemaet ovenfor til å legge til det første spillet.
            </p>
          </div>
        ) : (
          <div className="games-list">
            {sortedGames.map((game) => (
              <div key={game.id} className="game-row">
                <div className="game-row-left">
                  {/* Statusindikator — grønn for ferdig, gul for pågående */}
                  <span
                    className={`game-status-dot ${
                      game.isDone
                        ? "game-status-dot--done"
                        : "game-status-dot--active"
                    }`}
                    aria-hidden="true"
                  />
                  <span className="game-name">{game.name}</span>
                  <span className="game-type-badge">{game.gameType}</span>
                  {game.isDone && (
                    <span className="game-done-badge">Ferdig</span>
                  )}
                </div>

                {/* Lenke til spillets detaljside */}
                <Link
                  href={`/admin/tournaments/${tournament.id}/games/${game.id}`}
                  className="btn-open-game"
                >
                  {/* Pil-ikon */}
                  <svg
                    width="13"
                    height="13"
                    viewBox="0 0 13 13"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    aria-hidden="true"
                  >
                    <path
                      d="M2.5 6.5H10.5M10.5 6.5L7 3M10.5 6.5L7 10"
                      stroke="currentColor"
                      strokeWidth="1.25"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                  Åpne
                </Link>
              </div>
            ))}
          </div>
        )}
      </section>
    </>
  );
}
