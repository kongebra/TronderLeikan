// Lasteskjerm for turneringsdetaljsiden — vises mens server-side data hentes.
// Mimikker sidens faktiske layout med skeleton-elementer for minimal layout-skift.

// Skjelett-blokk — generisk plass-holder med konfigurerbar størrelse
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

// Skjelett for ett poengregelkort
function SkeletonPointCard() {
  return (
    <div
      style={{
        backgroundColor: "var(--color-bg-elevated)",
        border: "1px solid var(--color-border)",
        borderRadius: "0.625rem",
        padding: "1rem 1.25rem",
        display: "flex",
        alignItems: "center",
        gap: "0.875rem",
      }}
    >
      {/* Ikon-sirkel */}
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
        <SkeletonBlock width="55%" height="0.75rem" />
        <SkeletonBlock width="35%" height="1.125rem" />
      </div>
    </div>
  );
}

// Skjelett for én scoreboard-rad
function SkeletonScoreboardRow({ isTopThree }: { isTopThree?: boolean }) {
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: "2.5rem 1fr auto",
        alignItems: "center",
        gap: "1rem",
        padding: "0.75rem 1.25rem",
        backgroundColor: isTopThree ? "var(--color-bg-elevated)" : "transparent",
        borderRadius: isTopThree ? "0.5rem" : "0",
        borderBottom: !isTopThree ? "1px solid var(--color-border-subtle)" : "none",
        border: isTopThree ? "1px solid var(--color-border)" : undefined,
        marginBottom: isTopThree ? "0.5rem" : "0",
      }}
    >
      {/* Rang-sirkel */}
      <div
        style={{
          width: "1.875rem",
          height: "1.875rem",
          borderRadius: "50%",
          backgroundColor: isTopThree ? "var(--color-border)" : "var(--color-bg-overlay)",
          margin: "0 auto",
        }}
      />
      {/* Navn */}
      <SkeletonBlock width={isTopThree ? "65%" : "50%"} height="0.9375rem" />
      {/* Poeng */}
      <SkeletonBlock width="2.5rem" height={isTopThree ? "1.125rem" : "1rem"} />
    </div>
  );
}

// Antall poengregelkort og scoreboard-rader i skeleton
const POINT_RULE_COUNT = 7;
const SCOREBOARD_TOP_COUNT = 3;
const SCOREBOARD_REST_COUNT = 5;

export default function Loading() {
  return (
    <div className="container section">
      {/* Tilbake-lenke skeleton */}
      <div style={{ marginBottom: "2rem" }}>
        <SkeletonBlock width="8rem" height="0.875rem" />
      </div>

      {/* Hero-overskrift skeleton */}
      <div style={{ marginBottom: "3rem" }}>
        <SkeletonBlock
          width="5rem"
          height="1.25rem"
          borderRadius="9999px"
          style={{ marginBottom: "0.75rem" }}
        />
        <SkeletonBlock
          width="clamp(12rem, 40vw, 22rem)"
          height="clamp(1.75rem, 5vw, 3rem)"
          borderRadius="0.5rem"
          style={{ marginBottom: "0.75rem" }}
        />
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
          gap: "2rem",
          alignItems: "start",
        }}
      >
        {/* Poengregler-seksjon skeleton */}
        <div>
          <SkeletonBlock
            width="7rem"
            height="0.75rem"
            style={{ marginBottom: "1rem" }}
          />
          <div style={{ marginBottom: "1.25rem", display: "flex", flexDirection: "column", gap: "0.375rem" }}>
            <SkeletonBlock width="90%" height="0.75rem" />
            <SkeletonBlock width="70%" height="0.75rem" />
          </div>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(min(100%, 14rem), 1fr))",
              gap: "0.625rem",
            }}
          >
            {Array.from({ length: POINT_RULE_COUNT }).map((_, i) => (
              <SkeletonPointCard key={i} />
            ))}
          </div>
        </div>

        {/* Scoreboard-seksjon skeleton */}
        <div>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              marginBottom: "1rem",
            }}
          >
            <SkeletonBlock width="6rem" height="0.75rem" />
            <SkeletonBlock width="5rem" height="0.75rem" />
          </div>

          {/* Kolonneoverskrift-linje */}
          <div
            style={{
              borderBottom: "1px solid var(--color-border)",
              paddingBottom: "0.625rem",
              marginBottom: "0.75rem",
            }}
          />

          {/* Topp-3 rader */}
          {Array.from({ length: SCOREBOARD_TOP_COUNT }).map((_, i) => (
            <SkeletonScoreboardRow key={`top-${i}`} isTopThree />
          ))}

          {/* Resterende rader */}
          {Array.from({ length: SCOREBOARD_REST_COUNT }).map((_, i) => (
            <SkeletonScoreboardRow key={`rest-${i}`} />
          ))}
        </div>
      </div>
    </div>
  );
}
