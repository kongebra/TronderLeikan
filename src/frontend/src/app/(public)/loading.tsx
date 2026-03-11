// Lasteskjerm — vises av Next.js mens hjem-siden henter data server-side
// Bruker skeleton-kort for å gi bruker umiddelbar visuell respons

// Skjelett-kort som mimikker kortlayouten under lasting
function SkeletonCard() {
  return (
    <div
      style={{
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.75rem",
        padding: "1.5rem",
        overflow: "hidden",
      }}
    >
      {/* Aksent-stripe placeholder */}
      <div
        style={{
          height: "2px",
          width: "40%",
          backgroundColor: "var(--color-border)",
          borderRadius: "1px",
          marginBottom: "1.25rem",
        }}
      />
      {/* Tittellinje */}
      <div
        style={{
          height: "1.125rem",
          width: "75%",
          backgroundColor: "var(--color-border)",
          borderRadius: "0.25rem",
          marginBottom: "0.625rem",
        }}
      />
      {/* Undertekst-linje */}
      <div
        style={{
          height: "0.8125rem",
          width: "35%",
          backgroundColor: "var(--color-border-subtle)",
          borderRadius: "0.25rem",
        }}
      />
    </div>
  );
}

// Antall skjelett-kort som vises under lasting
const SKELETON_COUNT = 6;

export default function Loading() {
  return (
    <div className="container section">
      {/* Overskrift-skeleton */}
      <div style={{ marginBottom: "2.5rem" }}>
        <div
          style={{
            height: "1.25rem",
            width: "8rem",
            backgroundColor: "var(--color-border)",
            borderRadius: "9999px",
            marginBottom: "0.75rem",
          }}
        />
        <div
          style={{
            height: "2.5rem",
            width: "16rem",
            backgroundColor: "var(--color-border)",
            borderRadius: "0.375rem",
            marginBottom: "0.625rem",
          }}
        />
        <div
          style={{
            height: "1rem",
            width: "28rem",
            maxWidth: "100%",
            backgroundColor: "var(--color-border-subtle)",
            borderRadius: "0.25rem",
          }}
        />
      </div>

      {/* Kortgrid med skjelett-kort */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 20rem), 1fr))",
          gap: "1rem",
        }}
      >
        {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
          <SkeletonCard key={i} />
        ))}
      </div>
    </div>
  );
}
