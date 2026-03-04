namespace TronderLeikan.Application.Common.Interfaces;

// Abstraksjon mot bildeprosessering — Application kjenner ikke til ImageSharp
public interface IImageProcessor
{
    Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct);
    Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct);
}
