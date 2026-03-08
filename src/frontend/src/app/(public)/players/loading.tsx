// Lasteskjerm for spillerliste — vises av Next.js mens server-side data hentes.
// Mimikker kortgridet med skeleton-avatarer for minimal layout-skift.

// Skjelett for ett spillerkort
function SkeletonPlayerCard() {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: "0.875rem",
        padding: "1.5rem 1rem",
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.75rem",
        overflow: "hidden",
      }}
    >
      {/* Avatar-sirkel */}
      <div
        style={{
          width: "4rem",
          height: "4rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-bg-overlay)",
          flexShrink: 0,
        }}
      />

      {/* Navnelinje */}
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: "0.375rem",
          width: "100%",
        }}
      >
        <div
          style={{
            height: "0.9375rem",
            width: "70%",
            backgroundColor: "var(--color-border)",
            borderRadius: "0.25rem",
          }}
        />
        <div
          style={{
            height: "0.75rem",
            width: "40%",
            backgroundColor: "var(--color-border-subtle)",
            borderRadius: "0.25rem",
          }}
        />
      </div>
    </div>
  );
}

// Antall skjelett-kort under lasting — tilsvarer et realistisk antall spillere
const SKELETON_COUNT = 12;

export default function Loading() {
  return (
    <div className="container section">
      {/* Overskrift-skeleton */}
      <div style={{ marginBottom: "2.5rem" }}>
        <div
          style={{
            height: "1.25rem",
            width: "7rem",
            backgroundColor: "var(--color-border)",
            borderRadius: "9999px",
            marginBottom: "0.75rem",
          }}
        />
        <div
          style={{
            height: "2.5rem",
            width: "10rem",
            backgroundColor: "var(--color-border)",
            borderRadius: "0.375rem",
            marginBottom: "0.625rem",
          }}
        />
        <div
          style={{
            height: "1rem",
            width: "24rem",
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
          gridTemplateColumns:
            "repeat(auto-fill, minmax(min(100%, 10rem), 1fr))",
          gap: "1rem",
        }}
      >
        {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
          <SkeletonPlayerCard key={i} />
        ))}
      </div>
    </div>
  );
}
