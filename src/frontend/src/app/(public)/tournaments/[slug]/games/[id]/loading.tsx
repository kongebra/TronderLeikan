// Lasteskjerm for spilldetaljsiden — vises mens server-side data hentes.
// Mimikker sidens faktiske layout med skeleton-elementer for minimal layout-skift.

// Skjelett-blokk — generisk plasseholder med konfigurerbar størrelse
function SkeletonBlock({
  width,
  height,
  borderRadius = "0.25rem",
  style,
}: {
  width: string;
  height: string;
  borderRadius?: string;
  style?: React.CSSProperties;
}) {
  return (
    <div
      style={{
        width,
        height,
        borderRadius,
        backgroundColor: "var(--color-border)",
        maxWidth: "100%",
        ...style,
      }}
    />
  );
}

// Skjelett for én badge-pill
function SkeletonBadge({ width = "4rem" }: { width?: string }) {
  return (
    <SkeletonBlock
      width={width}
      height="1.375rem"
      borderRadius="9999px"
    />
  );
}

// Skjelett for én plassering med ikon og navn
function SkeletonPlacementRow() {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "1rem",
        padding: "0.875rem 1.25rem",
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.625rem",
      }}
    >
      {/* Plasseringsikon */}
      <div
        style={{
          width: "2.25rem",
          height: "2.25rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-bg-overlay)",
          flexShrink: 0,
        }}
      />
      <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: "0.375rem" }}>
        <SkeletonBlock width="3.5rem" height="0.625rem" />
        <SkeletonBlock width="55%" height="1rem" />
      </div>
    </div>
  );
}

// Skjelett for én rad i deltakerlisten
function SkeletonPersonRow() {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "0.625rem",
        padding: "0.5rem 0.75rem",
        borderRadius: "0.375rem",
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border-subtle)",
      }}
    >
      {/* Initialer-avatar */}
      <div
        style={{
          width: "1.75rem",
          height: "1.75rem",
          borderRadius: "50%",
          backgroundColor: "var(--color-bg-overlay)",
          flexShrink: 0,
        }}
      />
      <SkeletonBlock width="45%" height="0.875rem" />
    </div>
  );
}

// Antall skeleton-elementer per gruppe
const PLACEMENT_COUNT = 3;
const PARTICIPANT_COUNT = 5;
const ORGANIZER_COUNT = 2;

export default function Loading() {
  return (
    <div className="container section">
      {/* Tilbake-lenke skeleton */}
      <div style={{ marginBottom: "2rem" }}>
        <SkeletonBlock width="10rem" height="0.875rem" />
      </div>

      {/* Hero-overskrift skeleton */}
      <div style={{ marginBottom: "3rem" }}>
        {/* Badges */}
        <div style={{ display: "flex", gap: "0.5rem", marginBottom: "0.875rem" }}>
          <SkeletonBadge width="4.5rem" />
          <SkeletonBadge width="3.5rem" />
        </div>

        {/* Spillnavn */}
        <SkeletonBlock
          width="clamp(12rem, 40vw, 22rem)"
          height="clamp(1.75rem, 5vw, 3rem)"
          borderRadius="0.5rem"
          style={{ marginBottom: "0.75rem" }}
        />

        {/* Beskrivelseslinjer */}
        <div style={{ display: "flex", flexDirection: "column", gap: "0.375rem", marginTop: "0.625rem" }}>
          <SkeletonBlock width="80%" height="0.875rem" />
          <SkeletonBlock width="60%" height="0.875rem" />
        </div>

        {/* Dekorativ linje */}
        <SkeletonBlock
          width="24rem"
          height="1px"
          borderRadius="0"
          style={{ marginTop: "1.25rem", backgroundColor: "var(--color-border-subtle)" }}
        />
      </div>

      {/* Todelt layout skeleton */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 28rem), 1fr))",
          gap: "2.5rem",
          alignItems: "start",
        }}
      >
        {/* Plasseringsseksjon skeleton */}
        <div>
          <SkeletonBlock width="7rem" height="0.75rem" style={{ marginBottom: "0.875rem" }} />
          <div style={{ display: "flex", flexDirection: "column", gap: "0.625rem" }}>
            {Array.from({ length: PLACEMENT_COUNT }).map((_, i) => (
              <SkeletonPlacementRow key={i} />
            ))}
          </div>
        </div>

        {/* Deltakerlister skeleton */}
        <div style={{ display: "flex", flexDirection: "column", gap: "2rem" }}>
          {/* Deltakere */}
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: "0.625rem", marginBottom: "0.875rem" }}>
              <SkeletonBlock width="5.5rem" height="0.75rem" />
              <SkeletonBlock width="1.25rem" height="1rem" borderRadius="9999px" />
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: "0.375rem" }}>
              {Array.from({ length: PARTICIPANT_COUNT }).map((_, i) => (
                <SkeletonPersonRow key={i} />
              ))}
            </div>
          </div>

          {/* Arrangører */}
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: "0.625rem", marginBottom: "0.875rem" }}>
              <SkeletonBlock width="6rem" height="0.75rem" />
              <SkeletonBlock width="1rem" height="1rem" borderRadius="9999px" />
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: "0.375rem" }}>
              {Array.from({ length: ORGANIZER_COUNT }).map((_, i) => (
                <SkeletonPersonRow key={i} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
