import { headers } from "next/headers";
import { redirect } from "next/navigation";
import Link from "next/link";
import { auth } from "@/lib/auth";
import { LogoutButton } from "@/components/LogoutButton";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: {
    default: "Admin",
    template: "%s — Admin",
  },
};

// Admin-layoutnavigasjonslenker
const adminNavLinks = [
  {
    href: "/admin",
    label: "Dashboard",
    icon: (
      <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        <rect
          x="1.5"
          y="1.5"
          width="5.5"
          height="5.5"
          rx="1"
          stroke="currentColor"
          strokeWidth="1.25"
        />
        <rect
          x="9"
          y="1.5"
          width="5.5"
          height="5.5"
          rx="1"
          stroke="currentColor"
          strokeWidth="1.25"
        />
        <rect
          x="1.5"
          y="9"
          width="5.5"
          height="5.5"
          rx="1"
          stroke="currentColor"
          strokeWidth="1.25"
        />
        <rect
          x="9"
          y="9"
          width="5.5"
          height="5.5"
          rx="1"
          stroke="currentColor"
          strokeWidth="1.25"
        />
      </svg>
    ),
  },
  {
    href: "/admin/tournaments",
    label: "Turneringer",
    icon: (
      <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        <path
          d="M8 1.5L10.06 5.66L14.66 6.36L11.33 9.6L12.12 14.18L8 12.01L3.88 14.18L4.67 9.6L1.34 6.36L5.94 5.66L8 1.5Z"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinejoin="round"
        />
      </svg>
    ),
  },
  {
    href: "/admin/persons",
    label: "Spillere",
    icon: (
      <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        <circle
          cx="8"
          cy="5.5"
          r="3"
          stroke="currentColor"
          strokeWidth="1.25"
        />
        <path
          d="M2 13.5C2 11.015 4.686 9 8 9C11.314 9 14 11.015 14 13.5"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinecap="round"
        />
      </svg>
    ),
  },
] as const;

// Admin-layout med sesjonsbeskyttelse — omdirigerer ikke-autentiserte til /login
export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Sjekker sesjon på serveren — better-auth leser cookie fra request-headers
  const session = await auth.api.getSession({ headers: await headers() });

  if (!session) {
    redirect("/login");
  }

  // Brukervisningsnavn — prioriterer navn, faller tilbake til e-post
  const displayName = session.user.name || session.user.email;
  // Initialer for avataren — opp til 2 tegn fra navn eller e-post
  const initials = (session.user.name || session.user.email)
    .split(/[\s@]/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");

  return (
    <>
      <style>{`
        /* ============================================================
           ADMIN-LAYOUT — sidebar + innholdsområde
           ============================================================ */

        .admin-shell {
          display: grid;
          grid-template-columns: 220px 1fr;
          min-height: calc(100dvh - var(--header-height, 0px));
        }

        /* Sidebar */
        .admin-sidebar {
          display: flex;
          flex-direction: column;
          background-color: var(--color-bg-elevated);
          border-right: 1px solid var(--color-border);
          padding: 1.5rem 0;
          position: sticky;
          top: 0;
          height: 100dvh;
          overflow-y: auto;
        }

        /* Sidebar-logo / seksjonstittel */
        .admin-sidebar-brand {
          padding: 0 1rem 1.25rem;
          border-bottom: 1px solid var(--color-border-subtle);
          margin-bottom: 0.5rem;
        }

        .admin-sidebar-label {
          font-size: 0.6875rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-accent);
        }

        .admin-sidebar-title {
          font-size: 0.9375rem;
          font-weight: 700;
          color: var(--color-text-primary);
          letter-spacing: -0.02em;
          margin-top: 0.125rem;
        }

        /* Navigasjonsseksjon */
        .admin-nav {
          flex: 1;
          padding: 0.25rem 0.75rem;
        }

        .admin-nav-list {
          list-style: none;
          margin: 0;
          padding: 0;
          display: flex;
          flex-direction: column;
          gap: 2px;
        }

        /* Navigasjonslenke — basistilstand */
        .admin-nav-link {
          display: flex;
          align-items: center;
          gap: 0.625rem;
          padding: 0.5rem 0.625rem;
          border-radius: 0.5rem;
          text-decoration: none;
          font-size: 0.875rem;
          font-weight: 500;
          color: var(--color-text-secondary);
          transition:
            color 0.18s var(--ease-out-expo),
            background-color 0.18s var(--ease-out-expo);
        }

        .admin-nav-link:hover {
          color: var(--color-text-primary);
          background-color: var(--color-bg-overlay);
        }

        .admin-nav-link:hover .admin-nav-icon {
          color: var(--color-accent);
        }

        .admin-nav-icon {
          flex-shrink: 0;
          color: var(--color-text-muted);
          transition: color 0.18s var(--ease-out-expo);
        }

        /* Brukerprofil-seksjon nederst i sidebar */
        .admin-sidebar-user {
          margin-top: auto;
          padding: 1rem 0.75rem 0.5rem;
          border-top: 1px solid var(--color-border-subtle);
          display: flex;
          flex-direction: column;
          gap: 0.75rem;
        }

        .admin-user-info {
          display: flex;
          align-items: center;
          gap: 0.625rem;
        }

        /* Avatar med initialer */
        .admin-user-avatar {
          width: 2rem;
          height: 2rem;
          border-radius: 50%;
          background-color: var(--color-accent-subtle);
          border: 1px solid var(--color-accent-glow);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 0.6875rem;
          font-weight: 700;
          letter-spacing: 0.02em;
          color: var(--color-accent);
          flex-shrink: 0;
        }

        .admin-user-name {
          font-size: 0.8125rem;
          font-weight: 600;
          color: var(--color-text-primary);
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
          max-width: 130px;
        }

        /* Utloggingsknapp */
        .admin-logout-btn {
          display: flex;
          align-items: center;
          gap: 0.5rem;
          width: 100%;
          padding: 0.4375rem 0.625rem;
          border-radius: 0.5rem;
          border: 1px solid var(--color-border-subtle);
          background: transparent;
          color: var(--color-text-secondary);
          font-size: 0.8125rem;
          font-weight: 500;
          font-family: var(--font-sans);
          cursor: pointer;
          transition:
            color 0.18s var(--ease-out-expo),
            background-color 0.18s var(--ease-out-expo),
            border-color 0.18s var(--ease-out-expo);
          text-align: left;
        }

        .admin-logout-btn:hover {
          color: var(--color-danger);
          background-color: rgba(192, 57, 43, 0.07);
          border-color: rgba(192, 57, 43, 0.25);
        }

        .admin-logout-icon {
          display: flex;
          align-items: center;
          flex-shrink: 0;
        }

        /* Innholdsområde */
        .admin-content {
          overflow: auto;
          background-color: var(--color-bg-base);
        }

        .admin-content-inner {
          padding: 2rem;
          max-width: 1100px;
        }

        /* Responsiv — sidebar kollapser under 768px */
        @media (max-width: 767px) {
          .admin-shell {
            grid-template-columns: 1fr;
          }

          .admin-sidebar {
            position: static;
            height: auto;
            border-right: none;
            border-bottom: 1px solid var(--color-border);
            flex-direction: row;
            padding: 0.75rem;
            overflow-x: auto;
            overflow-y: visible;
          }

          .admin-sidebar-brand {
            display: none;
          }

          .admin-nav {
            padding: 0;
            flex: unset;
          }

          .admin-nav-list {
            flex-direction: row;
            gap: 4px;
          }

          .admin-sidebar-user {
            margin-top: 0;
            margin-left: auto;
            padding: 0;
            border-top: none;
            border-left: 1px solid var(--color-border-subtle);
            padding-left: 0.75rem;
            flex-direction: row;
            align-items: center;
            gap: 0.5rem;
            flex-shrink: 0;
          }
        }
      `}</style>

      <div className="admin-shell">
        {/* Venstresidig navigasjonslinje */}
        <aside className="admin-sidebar" aria-label="Admin-navigasjon">
          {/* Merkevare / seksjonstittel */}
          <div className="admin-sidebar-brand">
            <p className="admin-sidebar-label">TrønderLeikan</p>
            <p className="admin-sidebar-title">Administrasjon</p>
          </div>

          {/* Primærnavigasjon */}
          <nav className="admin-nav">
            <ul className="admin-nav-list" role="list">
              {adminNavLinks.map(({ href, label, icon }) => (
                <li key={href}>
                  <Link href={href} className="admin-nav-link">
                    <span className="admin-nav-icon">{icon}</span>
                    <span>{label}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </nav>

          {/* Innlogget bruker + utlogging */}
          <div className="admin-sidebar-user">
            <div className="admin-user-info">
              <div className="admin-user-avatar" aria-hidden="true">
                {initials}
              </div>
              <span className="admin-user-name" title={displayName}>
                {displayName}
              </span>
            </div>
            <LogoutButton />
          </div>
        </aside>

        {/* Hoved-innholdsområde */}
        <div className="admin-content">
          <div className="admin-content-inner animate-fade-up">{children}</div>
        </div>
      </div>
    </>
  );
}
