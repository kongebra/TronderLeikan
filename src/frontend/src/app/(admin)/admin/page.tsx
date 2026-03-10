import Link from "next/link";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Dashboard",
};

// Hurtiglenke-kort for de to admin-seksjonene
const sections = [
  {
    href: "/admin/tournaments",
    label: "Turneringer",
    description: "Administrer turneringer, runder og spill.",
    icon: (
      <svg
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        <path
          d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    ),
  },
  {
    href: "/admin/persons",
    label: "Spillere",
    description: "Legg til, rediger og administrer spillerregistre.",
    icon: (
      <svg
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        <circle
          cx="12"
          cy="8"
          r="4"
          stroke="currentColor"
          strokeWidth="1.5"
        />
        <path
          d="M4 20C4 16.686 7.582 14 12 14C16.418 14 20 16.686 20 20"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
      </svg>
    ),
  },
] as const;

// Admin-dashbord — oversiktsside med snarveier til de to hoveddelseksjonene
export default function AdminDashboardPage() {
  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN-DASHBORD — overskrift og snarveikort
           ============================================================ */

        .dashboard-header {
          margin-bottom: 2.5rem;
        }

        .dashboard-eyebrow {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
          margin-bottom: 0.5rem;
        }

        .dashboard-title {
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.1;
          margin: 0 0 0.5rem;
        }

        .dashboard-subtitle {
          font-size: 0.9375rem;
          color: var(--color-text-secondary);
          margin: 0;
        }

        /* Aksent-skillelinje under overskriften */
        .dashboard-divider {
          width: 3rem;
          height: 2px;
          background: linear-gradient(
            90deg,
            var(--color-accent) 0%,
            transparent 100%
          );
          border: none;
          margin: 1.25rem 0 2.5rem;
        }

        /* Kortgrid */
        .dashboard-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
          gap: 1rem;
        }

        /* Snarveikort */
        .dashboard-card {
          display: flex;
          flex-direction: column;
          gap: 1rem;
          padding: 1.5rem;
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 0.75rem;
          text-decoration: none;
          color: inherit;
          transition:
            border-color 0.2s var(--ease-out-expo),
            background-color 0.2s var(--ease-out-expo),
            transform 0.2s var(--ease-out-expo);
          position: relative;
          overflow: hidden;
        }

        /* Subtil aksent-glød øverst i kortet */
        .dashboard-card::before {
          content: "";
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          height: 1px;
          background: linear-gradient(
            90deg,
            transparent,
            var(--color-accent-glow),
            transparent
          );
          opacity: 0;
          transition: opacity 0.2s var(--ease-out-expo);
        }

        .dashboard-card:hover {
          border-color: var(--color-accent);
          background-color: var(--color-bg-overlay);
          transform: translateY(-2px);
        }

        .dashboard-card:hover::before {
          opacity: 1;
        }

        .dashboard-card-icon {
          display: flex;
          align-items: center;
          justify-content: center;
          width: 2.5rem;
          height: 2.5rem;
          border-radius: 0.5rem;
          background-color: var(--color-accent-subtle);
          border: 1px solid var(--color-accent-glow);
          color: var(--color-accent);
          flex-shrink: 0;
        }

        .dashboard-card-body {
          flex: 1;
        }

        .dashboard-card-label {
          font-size: 1rem;
          font-weight: 700;
          letter-spacing: -0.02em;
          color: var(--color-text-primary);
          margin: 0 0 0.25rem;
        }

        .dashboard-card-desc {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
          margin: 0;
          line-height: 1.5;
        }

        /* Pil-indikator */
        .dashboard-card-arrow {
          align-self: flex-end;
          color: var(--color-text-muted);
          transition: color 0.2s var(--ease-out-expo), transform 0.2s var(--ease-out-expo);
        }

        .dashboard-card:hover .dashboard-card-arrow {
          color: var(--color-accent);
          transform: translateX(3px);
        }
      `}</style>

      <div>
        {/* Sideoverskrift */}
        <header className="dashboard-header">
          <p className="dashboard-eyebrow">Administrasjon</p>
          <h1 className="dashboard-title">Dashboard</h1>
          <p className="dashboard-subtitle">
            Administrer turneringer og spillere for TrønderLeikan.
          </p>
        </header>

        <hr className="dashboard-divider" />

        {/* Snarveikort */}
        <div className="dashboard-grid">
          {sections.map(({ href, label, description, icon }) => (
            <Link key={href} href={href} className="dashboard-card">
              <div className="dashboard-card-icon">{icon}</div>
              <div className="dashboard-card-body">
                <h2 className="dashboard-card-label">{label}</h2>
                <p className="dashboard-card-desc">{description}</p>
              </div>
              <span className="dashboard-card-arrow" aria-hidden="true">
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 16 16"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    d="M3 8H13M13 8L9 4M13 8L9 12"
                    stroke="currentColor"
                    strokeWidth="1.25"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </span>
            </Link>
          ))}
        </div>
      </div>
    </>
  );
}
