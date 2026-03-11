"use client";

// Feilgrense for hjem-siden — vises hvis siden kaster en uventet feil
// Merk: Error Boundary i Next.js må være en Client Component

type ErrorPageProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function Error({ error, reset }: ErrorPageProps) {
  return (
    <div className="container section">
      <div
        className="animate-fade-up"
        style={{
          maxWidth: "32rem",
          margin: "0 auto",
          textAlign: "center",
          padding: "4rem 2rem",
          border: "1px solid var(--color-danger)",
          borderRadius: "0.75rem",
          backgroundColor: "var(--color-bg-elevated)",
        }}
      >
        {/* Feil-ikon */}
        <div
          aria-hidden="true"
          style={{
            fontSize: "2rem",
            marginBottom: "1rem",
            color: "var(--color-danger)",
          }}
        >
          ⚠
        </div>

        {/* Overskrift */}
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: 700,
            letterSpacing: "-0.02em",
            color: "var(--color-text-primary)",
            marginBottom: "0.5rem",
          }}
        >
          Kunne ikke laste turneringer
        </h2>

        {/* Feilmelding */}
        <p
          style={{
            fontSize: "0.875rem",
            color: "var(--color-text-secondary)",
            marginBottom: "1.5rem",
            lineHeight: 1.6,
          }}
        >
          {error.message
            ? error.message
            : "En uventet feil oppstod. Prøv igjen om litt."}
        </p>

        {/* Prøv-igjen-knapp */}
        <button
          onClick={reset}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "0.5rem",
            padding: "0.5rem 1.25rem",
            borderRadius: "0.5rem",
            backgroundColor: "var(--color-accent-subtle)",
            color: "var(--color-accent)",
            border: "1px solid var(--color-accent-glow)",
            fontFamily: "inherit",
            fontSize: "0.875rem",
            fontWeight: 600,
            cursor: "pointer",
            transition: "background-color 0.2s var(--ease-out-expo)",
          }}
          onMouseOver={(e) => {
            (e.currentTarget as HTMLButtonElement).style.backgroundColor =
              "var(--color-bg-overlay)";
          }}
          onMouseOut={(e) => {
            (e.currentTarget as HTMLButtonElement).style.backgroundColor =
              "var(--color-accent-subtle)";
          }}
        >
          Prøv igjen
        </button>
      </div>
    </div>
  );
}
