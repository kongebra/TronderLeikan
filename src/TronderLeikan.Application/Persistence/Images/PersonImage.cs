namespace TronderLeikan.Application.Persistence.Images;

// Infrastruktur-entitet — ikke et domenekonsept
// Bilder holdes i en separat tabell for å unngå at bytes lastes ved vanlige Person-queries
public sealed class PersonImage
{
    public Guid PersonId { get; set; }
    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "image/webp";
}
