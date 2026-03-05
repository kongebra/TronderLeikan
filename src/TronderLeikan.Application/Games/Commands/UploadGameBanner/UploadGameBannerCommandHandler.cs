using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Application.Games.Commands.UploadGameBanner;

public sealed class UploadGameBannerCommandHandler(IAppDbContext db, IImageProcessor imageProcessor)
    : ICommandHandler<UploadGameBannerCommand>
{
    public async Task<Result> Handle(UploadGameBannerCommand command, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([command.GameId], ct);
        if (game is null) return Result.Fail($"Spill {command.GameId} finnes ikke.");

        var bytes = await imageProcessor.ProcessGameBannerAsync(command.BannerStream, ct);

        var existing = await db.GameBanners.FindAsync([command.GameId], ct);
        if (existing is not null)
        {
            existing.ImageData = bytes;
        }
        else
        {
            db.GameBanners.Add(new GameBanner
            {
                GameId = command.GameId,
                ImageData = bytes,
                ContentType = "image/webp"
            });
        }

        game.SetBanner();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
