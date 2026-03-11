"use client";

import { authClient } from "@/lib/auth-client";
import { useState } from "react";

// Innloggingsside — klientkomponent siden den kaller authClient.signIn.social()
export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);

  async function handleSignIn() {
    setIsLoading(true);
    try {
      // Starter Zitadel OIDC-innloggingsflyt med omdirigering til /admin etter suksess
      await authClient.signIn.social({
        provider: "zitadel",
        callbackURL: "/admin",
      });
    } catch {
      // Feil ved innlogging — tilbakestill lastestatus
      setIsLoading(false);
    }
  }

  return (
    <>
      <style>{`
        /* ============================================================
           INNLOGGINGSSIDE — sentrert, mørk, nordisk editorial
           ============================================================ */

        .login-page {
          min-height: 100dvh;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 2rem;
          position: relative;
          overflow: hidden;
        }

        /* Bakgrunns-lyseffekt — subtil stråle fra topp-venstre */
        .login-page::before {
          content: "";
          position: fixed;
          top: -30%;
          left: -20%;
          width: 60%;
          height: 60%;
          background: radial-gradient(
            ellipse at center,
            var(--color-accent-glow) 0%,
            transparent 70%
          );
          pointer-events: none;
          z-index: 0;
        }

        /* Dekorativ vertikal linje */
        .login-page::after {
          content: "";
          position: fixed;
          top: 0;
          left: 50%;
          width: 1px;
          height: 100%;
          background: linear-gradient(
            180deg,
            transparent 0%,
            var(--color-border-subtle) 30%,
            var(--color-border-subtle) 70%,
            transparent 100%
          );
          pointer-events: none;
          z-index: 0;
          opacity: 0.4;
        }

        /* Hovud-kort */
        .login-card {
          position: relative;
          z-index: 1;
          width: 100%;
          max-width: 400px;
          background-color: var(--color-bg-elevated);
          border: 1px solid var(--color-border);
          border-radius: 1rem;
          padding: 2.5rem 2rem;
          animation: fade-up 0.5s var(--ease-out-expo) both;
        }

        /* Toppskillelinje med aksent-glød */
        .login-card::before {
          content: "";
          position: absolute;
          top: 0;
          left: 2rem;
          right: 2rem;
          height: 1px;
          background: linear-gradient(
            90deg,
            transparent,
            var(--color-accent),
            transparent
          );
        }

        /* Logo-seksjon */
        .login-logo {
          display: flex;
          align-items: center;
          gap: 0.625rem;
          margin-bottom: 2rem;
        }

        .login-logo-mark {
          font-size: 1.75rem;
          color: var(--color-accent);
          line-height: 1;
          display: inline-block;
          animation: rotate-in 0.6s var(--ease-out-expo) 0.1s both;
        }

        .login-logo-text {
          font-size: 1.125rem;
          font-weight: 700;
          letter-spacing: -0.03em;
          color: var(--color-text-primary);
        }

        @keyframes rotate-in {
          from {
            opacity: 0;
            transform: rotate(-90deg) scale(0.5);
          }
          to {
            opacity: 1;
            transform: rotate(0deg) scale(1);
          }
        }

        /* Overskrift og ingress */
        .login-heading {
          font-size: 1.5rem;
          font-weight: 800;
          letter-spacing: -0.04em;
          color: var(--color-text-primary);
          line-height: 1.15;
          margin: 0 0 0.5rem;
        }

        .login-subheading {
          font-size: 0.9rem;
          color: var(--color-text-secondary);
          margin: 0 0 2rem;
          line-height: 1.55;
        }

        /* Innloggingsknapp */
        .login-btn {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 0.625rem;
          width: 100%;
          padding: 0.75rem 1.5rem;
          border-radius: 0.625rem;
          border: 1px solid var(--color-accent);
          background-color: var(--color-accent-subtle);
          color: var(--color-accent);
          font-family: var(--font-sans);
          font-size: 0.9375rem;
          font-weight: 600;
          letter-spacing: -0.01em;
          cursor: pointer;
          transition:
            background-color 0.2s var(--ease-out-expo),
            border-color 0.2s var(--ease-out-expo),
            color 0.2s var(--ease-out-expo),
            transform 0.15s var(--ease-out-expo),
            box-shadow 0.2s var(--ease-out-expo);
          position: relative;
          overflow: hidden;
        }

        .login-btn:hover:not(:disabled) {
          background-color: var(--color-accent);
          color: #0c0d10;
          box-shadow: 0 0 24px var(--color-accent-glow);
          transform: translateY(-1px);
        }

        .login-btn:active:not(:disabled) {
          transform: translateY(0);
        }

        .login-btn:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        /* Laste-spinner */
        .login-spinner {
          width: 16px;
          height: 16px;
          border: 2px solid currentColor;
          border-top-color: transparent;
          border-radius: 50%;
          animation: spin 0.6s linear infinite;
          flex-shrink: 0;
        }

        @keyframes spin {
          to { transform: rotate(360deg); }
        }

        /* Zitadel-ikon */
        .login-provider-icon {
          display: flex;
          align-items: center;
          flex-shrink: 0;
        }

        /* Dekorativt sitatavsnitt */
        .login-footnote {
          margin-top: 1.5rem;
          padding-top: 1.25rem;
          border-top: 1px solid var(--color-border-subtle);
          font-size: 0.8125rem;
          color: var(--color-text-muted);
          text-align: center;
          line-height: 1.5;
        }

        .login-footnote strong {
          color: var(--color-text-secondary);
        }
      `}</style>

      <div className="login-page">
        <div className="noise-overlay" aria-hidden="true" />

        <div className="login-card" role="main">
          {/* Logo */}
          <div className="login-logo" aria-label="TrønderLeikan">
            <span className="login-logo-mark" aria-hidden="true">⬡</span>
            <span className="login-logo-text">TrønderLeikan</span>
          </div>

          {/* Overskrift */}
          <h1 className="login-heading">Adminpanel</h1>
          <p className="login-subheading">
            Logg inn med din organisasjonskonto for å administrere turneringer
            og spillere.
          </p>

          {/* Innloggingsknapp — starter Zitadel OIDC-flyten */}
          <button
            className="login-btn"
            onClick={handleSignIn}
            disabled={isLoading}
            aria-busy={isLoading}
          >
            {isLoading ? (
              <>
                <span className="login-spinner" aria-hidden="true" />
                <span>Logger inn...</span>
              </>
            ) : (
              <>
                <span className="login-provider-icon" aria-hidden="true">
                  <svg
                    width="18"
                    height="18"
                    viewBox="0 0 18 18"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M9 1L16 5V13L9 17L2 13V5L9 1Z"
                      stroke="currentColor"
                      strokeWidth="1.4"
                      strokeLinejoin="round"
                    />
                    <circle cx="9" cy="9" r="2.5" fill="currentColor" />
                  </svg>
                </span>
                <span>Logg inn med Zitadel</span>
              </>
            )}
          </button>

          {/* Bunntekst */}
          <p className="login-footnote">
            <strong>TrønderLeikan</strong> — Turneringer og poengberegning
            i&nbsp;Trøndelag
          </p>
        </div>
      </div>
    </>
  );
}
