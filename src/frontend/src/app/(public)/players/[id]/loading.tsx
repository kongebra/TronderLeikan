// Lasteskjerm for spillerprofil — vises mens server-side data hentes.
// Mimikker profilsidenes layout med skeleton-elementer for minimalt layout-skift.

// Skjelett-blokk — generisk plassholder med konfigurerbar størrelse
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

export default function Loading() {
  return (
    <div className="container section">
      {/* Tilbake-lenke skeleton */}
      <div style={{ marginBottom: "2rem" }}>
        <SkeletonBlock width="8rem" height="0.875rem" />
      </div>

      {/* Profilhero skeleton */}
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "flex-start",
          gap: "2rem",
          marginBottom: "3rem",
        }}
      >
        {/* Avatar-sirkel */}
        <div
          style={{
            width: "7.5rem",
            height: "7.5rem",
            borderRadius: "50%",
            backgroundColor: "var(--color-bg-overlay)",
            border: "2px solid var(--color-border)",
            flexShrink: 0,
          }}
        />

        {/* Navneblokk */}
        <div
          style={{ display: "flex", flexDirection: "column", gap: "0.625rem" }}
        >
          {/* Badge */}
          <SkeletonBlock
            width="5rem"
            height="1.25rem"
            borderRadius="9999px"
            style={{ marginBottom: "0.125rem" }}
          />
          {/* Navn */}
          <SkeletonBlock
            width="clamp(10rem, 35vw, 18rem)"
            height="clamp(1.75rem, 5vw, 3rem)"
            borderRadius="0.5rem"
          />
          {/* Aksent-linje */}
          <SkeletonBlock
            width="20rem"
            height="1px"
            borderRadius="0"
            style={{
              marginTop: "0.75rem",
              backgroundColor: "var(--color-border-subtle)",
            }}
          />
        </div>
      </div>
    </div>
  );
}
