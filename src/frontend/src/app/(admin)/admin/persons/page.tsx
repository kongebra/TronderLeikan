import type { Metadata } from "next";
import { createPersonAction, deletePersonAction } from "./actions";

// Datamodell for spilleroversikt — tilsvarer API-respons fra /api/v1/persons
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter alle spillere fra backend — returnerer tom liste ved feil slik at siden alltid rendres
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

export const metadata: Metadata = {
  title: "Spillere",
};

// Admin-side for spilleradministrasjon — CRUD for spillerregistre
export default async function AdminPersonsPage() {
  const persons = await getPersons();

  // Sorterer alfabetisk på etternavn, deretter fornavn
  const sorted = persons
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN SPILLERE — sideoverskrift, skjema og tabell
           ============================================================ */

        .persons-header {
          margin-bottom: 2rem;
        }

        .persons-eyebrow {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
          margin-bottom: 0.375rem;
        }

        .persons-title {
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.1;
          margin: 0 0 0.375rem;
        }

        .persons-subtitle {
          font-size: 0.9375rem;
          color: var(--color-text-secondary);
          margin: 0;
        }

        /* Aksent-skillelinje */
        .persons-divider {
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
          flex: 1 1 160px;
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
          color: #0c0d10;
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

        /* ---- Tabellseksjon ---- */

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
        .persons-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 0.875rem;
        }

        .persons-table thead tr {
          border-bottom: 1px solid var(--color-border-subtle);
        }

        .persons-table th {
          padding: 0.625rem 1.25rem;
          text-align: left;
          font-size: 0.6875rem;
          font-weight: 700;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          color: var(--color-text-muted);
          white-space: nowrap;
        }

        .persons-table td {
          padding: 0.875rem 1.25rem;
          color: var(--color-text-secondary);
          border-bottom: 1px solid var(--color-border-subtle);
          vertical-align: middle;
        }

        .persons-table tbody tr:last-child td {
          border-bottom: none;
        }

        /* Uthev rad ved hover */
        .persons-table tbody tr {
          transition: background-color 0.15s var(--ease-out-expo);
        }

        .persons-table tbody tr:hover {
          background-color: var(--color-bg-overlay);
        }

        /* Navn-kolonne — markert tekst */
        .td-name {
          color: var(--color-text-primary);
          font-weight: 600;
          letter-spacing: -0.01em;
        }

        /* Bilde-status — badge */
        .td-image-yes {
          display: inline-flex;
          align-items: center;
          gap: 0.3rem;
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.125rem 0.5rem;
          border-radius: 9999px;
          background-color: rgba(61, 158, 110, 0.12);
          color: var(--color-success);
          border: 1px solid rgba(61, 158, 110, 0.25);
        }

        .td-image-no {
          display: inline-flex;
          align-items: center;
          gap: 0.3rem;
          font-size: 0.75rem;
          font-weight: 600;
          padding: 0.125rem 0.5rem;
          border-radius: 9999px;
          background-color: var(--color-bg-overlay);
          color: var(--color-text-muted);
          border: 1px solid var(--color-border-subtle);
        }

        /* ID-kolonne — monospace-lignende */
        .td-id {
          font-size: 0.75rem;
          font-family: "Courier New", Courier, monospace;
          color: var(--color-text-muted);
          white-space: nowrap;
        }

        /* Slett-knapp */
        .btn-delete {
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
          transition:
            color 0.18s var(--ease-out-expo),
            background-color 0.18s var(--ease-out-expo),
            border-color 0.18s var(--ease-out-expo);
          display: inline-flex;
          align-items: center;
          gap: 0.35rem;
        }

        .btn-delete:hover {
          color: var(--color-danger);
          background-color: rgba(192, 57, 43, 0.07);
          border-color: rgba(192, 57, 43, 0.25);
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
      <header className="persons-header">
        <p className="persons-eyebrow">Administrasjon</p>
        <h1 className="persons-title">Spillere</h1>
        <p className="persons-subtitle">
          Opprett, vis og slett spillerprofiler i TrønderLeikan.
        </p>
      </header>

      <hr className="persons-divider" />

      {/* ---- Opprett ny spiller ---- */}
      <section className="create-panel" aria-labelledby="create-panel-title">
        <h2 className="create-panel-title" id="create-panel-title">
          Legg til ny spiller
        </h2>

        {/* Skjema bruker Server Action direkte — ingen klient-JS nødvendig */}
        <form action={createPersonAction} className="create-form-row">
          <div className="create-form-field">
            <label htmlFor="firstName" className="form-label">
              Fornavn
            </label>
            <input
              id="firstName"
              name="firstName"
              type="text"
              required
              placeholder="Ola"
              autoComplete="given-name"
              className="form-input"
            />
          </div>

          <div className="create-form-field">
            <label htmlFor="lastName" className="form-label">
              Etternavn
            </label>
            <input
              id="lastName"
              name="lastName"
              type="text"
              required
              placeholder="Nordmann"
              autoComplete="family-name"
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
            Opprett spiller
          </button>
        </form>
      </section>

      {/* ---- Spillerliste ---- */}
      <section
        className="table-section"
        aria-labelledby="players-table-title"
      >
        {/* Tabelloverskrift med teller */}
        <div className="table-header">
          <h2 className="table-header-title" id="players-table-title">
            Registrerte spillere
          </h2>
          {sorted.length > 0 && (
            <span className="table-count-badge" aria-live="polite">
              {sorted.length} {sorted.length === 1 ? "spiller" : "spillere"}
            </span>
          )}
        </div>

        {sorted.length === 0 ? (
          /* Tom-tilstand — ingen spillere ennå */
          <div className="empty-state">
            <div className="empty-state-icon" aria-hidden="true">
              ◈
            </div>
            <p className="empty-state-title">Ingen spillere registrert</p>
            <p className="empty-state-desc">
              Bruk skjemaet ovenfor til å legge til den første spilleren.
            </p>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="persons-table" aria-label="Spillerliste">
              <thead>
                <tr>
                  <th scope="col">Fornavn</th>
                  <th scope="col">Etternavn</th>
                  <th scope="col">Bilde</th>
                  <th scope="col">ID</th>
                  <th scope="col">
                    <span className="sr-only">Handlinger</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((person) => (
                  <tr key={person.id}>
                    {/* Fornavn */}
                    <td className="td-name">{person.firstName}</td>

                    {/* Etternavn */}
                    <td className="td-name">{person.lastName}</td>

                    {/* Profilbilde-status */}
                    <td>
                      {person.hasProfileImage ? (
                        <span className="td-image-yes" aria-label="Bilde lastet opp">
                          <svg
                            width="10"
                            height="10"
                            viewBox="0 0 10 10"
                            fill="none"
                            xmlns="http://www.w3.org/2000/svg"
                            aria-hidden="true"
                          >
                            <path
                              d="M2 5L4 7L8 3"
                              stroke="currentColor"
                              strokeWidth="1.5"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                          Ja
                        </span>
                      ) : (
                        <span className="td-image-no" aria-label="Ingen bilde">
                          <svg
                            width="10"
                            height="10"
                            viewBox="0 0 10 10"
                            fill="none"
                            xmlns="http://www.w3.org/2000/svg"
                            aria-hidden="true"
                          >
                            <path
                              d="M3 3L7 7M7 3L3 7"
                              stroke="currentColor"
                              strokeWidth="1.5"
                              strokeLinecap="round"
                            />
                          </svg>
                          Nei
                        </span>
                      )}
                    </td>

                    {/* Spiller-ID — forkortet for lesbarhet */}
                    <td className="td-id" title={person.id}>
                      {person.id.slice(0, 8)}…
                    </td>

                    {/* Slett-knapp via inline Server Action i form */}
                    <td>
                      <form
                        action={async () => {
                          "use server";
                          await deletePersonAction(person.id);
                        }}
                      >
                        <button
                          type="submit"
                          className="btn-delete"
                          aria-label={`Slett ${person.firstName} ${person.lastName}`}
                        >
                          {/* Søppelkasse-ikon */}
                          <svg
                            width="13"
                            height="13"
                            viewBox="0 0 13 13"
                            fill="none"
                            xmlns="http://www.w3.org/2000/svg"
                            aria-hidden="true"
                          >
                            <path
                              d="M2 3.5H11M4.5 3.5V2.5C4.5 2.224 4.724 2 5 2H8C8.276 2 8.5 2.224 8.5 2.5V3.5M5.5 6V9.5M7.5 6V9.5M3 3.5L3.5 10.5C3.5 10.776 3.724 11 4 11H9C9.276 11 9.5 10.776 9.5 10.5L10 3.5"
                              stroke="currentColor"
                              strokeWidth="1.25"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                          Slett
                        </button>
                      </form>
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
