namespace TronderLeikan.Application.Persistence.Images;

// Infrastruktur-entitet — ikke et domenekonsept
// Game-bannere lagres separat (opptil 1920x1080 WebP) for å unngå at bytes lastes ved Game-queries
public sealed class GameBanner
{
    public Guid GameId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}
