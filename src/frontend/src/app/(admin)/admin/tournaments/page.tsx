import type { Metadata } from "next";
import Link from "next/link";
import { createTournamentAction } from "./actions";

// Datamodell for turneringsoversikt — tilsvarer API-respons fra /api/v1/tournaments
type TournamentSummaryResponse = {
  id: string;
  name: string;
  slug: string;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter alle turneringer fra backend — returnerer tom liste ved feil slik at siden alltid rendres
async function getTournaments(): Promise<TournamentSummaryResponse[]> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/tournaments`, {
      cache: "no-store",
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Turneringer",
};

// Admin-side for turneringsadministrasjon — liste og opprett
export default async function AdminTournamentsPage() {
  const tournaments = await getTournaments();

  // Sorterer alfabetisk på navn
  const sorted = tournaments
    .slice()
    .sort((a, b) => a.name.localeCompare(b.name, "nb"));

  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN TURNERINGER — sideoverskrift, skjema og liste
           ============================================================ */

        .tournaments-header {
          margin-bottom: 2rem;
        }

        .tournaments-eyebrow {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
          margin-bottom: 0.375rem;
        }

        .tournaments-title {
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.1;
          margin: 0 0 0.375rem;
        }

        .tournaments-subtitle {
          font-size: 0.9375rem;
          color: var(--color-text-secondary);
          margin: 0;
        }

        /* Aksent-skillelinje */
        .tournaments-divider {
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

        /* ---- Opprett-skjema ---- */

        .create-panel {
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          padding: 1.5rem;
          margin-bottom: 2rem;
        }

        .create-panel-title {
          font-size: 0.875rem;
          font-weight: 700;
          letter-spacing: -0.01em;
          color: var(--color-text-primary);
          margin: 0 0 1rem;
          display: flex;
          align-items: center;
          gap: 0.5rem;
        }

        .create-panel-title::before {
          content: "";
          display: inline-block;
          width: 0.25rem;
          height: 1em;
          background-color: var(--color-accent);
          border-radius: 2px;
        }

        /* Skjema-rad med inngangsfelt og knapp */
        .create-form-row {
          display: flex;
          flex-wrap: wrap;
          gap: 0.75rem;
          align-items: flex-end;
        }

        .create-form-field {
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

        /* Primærknapp — aksent */
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

        /* ---- Turneringsliste ---- */

        .table-section {
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          overflow: hidden;
        }

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

        /* Tabellwrapper for horisontal rulling på mobil */
        .table-scroll {
          overflow-x: auto;
        }

        /* Selve tabellen */
        .tournaments-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 0.875rem;
        }

        .tournaments-table thead tr {
          border-bottom: 1px solid var(--color-border-subtle);
        }

        .tournaments-table th {
          padding: 0.625rem 1.25rem;
          text-align: left;
          font-size: 0.6875rem;
          font-weight: 700;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          color: var(--color-text-muted);
          white-space: nowrap;
        }

        .tournaments-table td {
          padding: 0.875rem 1.25rem;
          color: var(--color-text-secondary);
          border-bottom: 1px solid var(--color-border-subtle);
          vertical-align: middle;
        }

        .tournaments-table tbody tr:last-child td {
          border-bottom: none;
        }

        /* Uthev rad ved hover */
        .tournaments-table tbody tr {
          transition: background-color 0.15s var(--ease-out-expo);
        }

        .tournaments-table tbody tr:hover {
          background-color: var(--color-bg-overlay);
        }

        /* Navn-kolonne — markert tekst */
        .td-name {
          color: var(--color-text-primary);
          font-weight: 600;
          letter-spacing: -0.01em;
        }

        /* Slug-kolonne — monospace-lignende */
        .td-slug {
          font-size: 0.75rem;
          font-family: "Courier New", Courier, monospace;
          color: var(--color-text-muted);
        }

        /* ID-kolonne — forkortet */
        .td-id {
          font-size: 0.75rem;
          font-family: "Courier New", Courier, monospace;
          color: var(--color-text-muted);
          white-space: nowrap;
        }

        /* Åpne-lenke */
        .btn-open {
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
          white-space: nowrap;
        }

        .btn-open:hover {
          color: var(--color-accent);
          background-color: var(--color-accent-subtle);
          border-color: var(--color-accent-glow);
        }

        /* Tom-tilstand */
        .empty-state {
          padding: 3.5rem 2rem;
          text-align: center;
          color: var(--color-text-muted);
        }

        .empty-state-icon {
          font-size: 2rem;
          margin-bottom: 0.875rem;
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
      `}</style>

      {/* Sideoverskrift */}
      <header className="tournaments-header">
        <p className="tournaments-eyebrow">Administrasjon</p>
        <h1 className="tournaments-title">Turneringer</h1>
        <p className="tournaments-subtitle">
          Opprett og administrer turneringer og poengregler i TrønderLeikan.
        </p>
      </header>

      <hr className="tournaments-divider" />

      {/* ---- Opprett ny turnering ---- */}
      <section className="create-panel" aria-labelledby="create-panel-title">
        <h2 className="create-panel-title" id="create-panel-title">
          Opprett ny turnering
        </h2>

        {/* Skjema bruker Server Action direkte — ingen klient-JS nødvendig */}
        <form action={createTournamentAction} className="create-form-row">
          <div className="create-form-field">
            <label htmlFor="name" className="form-label">
              Navn
            </label>
            <input
              id="name"
              name="name"
              type="text"
              required
              placeholder="TrønderLeikan 2026"
              className="form-input"
            />
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
            Opprett turnering
          </button>
        </form>
      </section>

      {/* ---- Turneringsliste ---- */}
      <section className="table-section" aria-labelledby="tournaments-table-title">
        {/* Tabelloverskrift med teller */}
        <div className="table-header">
          <h2 className="table-header-title" id="tournaments-table-title">
            Registrerte turneringer
          </h2>
          {sorted.length > 0 && (
            <span className="table-count-badge" aria-live="polite">
              {sorted.length}{" "}
              {sorted.length === 1 ? "turnering" : "turneringer"}
            </span>
          )}
        </div>

        {sorted.length === 0 ? (
          /* Tom-tilstand — ingen turneringer ennå */
          <div className="empty-state">
            <div className="empty-state-icon" aria-hidden="true">
              ◈
            </div>
            <p className="empty-state-title">Ingen turneringer registrert</p>
            <p className="empty-state-desc">
              Bruk skjemaet ovenfor til å opprette den første turneringen.
            </p>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="tournaments-table" aria-label="Turneringsliste">
              <thead>
                <tr>
                  <th scope="col">Navn</th>
                  <th scope="col">Slug</th>
                  <th scope="col">ID</th>
                  <th scope="col">
                    <span className="sr-only">Handlinger</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((tournament) => (
                  <tr key={tournament.id}>
                    {/* Turneringsnavn */}
                    <td className="td-name">{tournament.name}</td>

                    {/* Slug — brukes i offentlig URL */}
                    <td className="td-slug">{tournament.slug}</td>

                    {/* Turnerings-ID — forkortet for lesbarhet */}
                    <td className="td-id" title={tournament.id}>
                      {tournament.id.slice(0, 8)}…
                    </td>

                    {/* Åpne-lenke til detaljside */}
                    <td>
                      <Link
                        href={`/admin/tournaments/${tournament.id}`}
                        className="btn-open"
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
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
