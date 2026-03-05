using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Services;

// Konverterer og resizeer bilder med ImageSharp — Application vet ikke om ImageSharp
internal sealed class ImageSharpImageProcessor : IImageProcessor
{
    // Profilbilder: 256×256 px, WebP
    public async Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(ctx => ctx.Resize(256, 256));
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder(), ct);
        return ms.ToArray();
    }

    // Spillbannere: 1200×400 px, WebP
    public async Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(ctx => ctx.Resize(1200, 400));
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder(), ct);
        return ms.ToArray();
    }
}
