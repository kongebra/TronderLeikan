using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Commands.UploadGameBanner;
using TronderLeikan.Application.Games.Queries.GetGameById;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Tests.Games;

public sealed class GameBannerAndQueryTests
{
    private sealed class FakeImageProcessor : IImageProcessor
    {
        public Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct) => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct) => Task.FromResult(new byte[] { 1, 2 });
    }

    [Fact]
    public async Task UploadGameBanner_LagrerBannerOgSetterFlag()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();
        using var stream = new MemoryStream([9]);
        var result = await new UploadGameBannerCommandHandler(db, new FakeImageProcessor()).Handle(new UploadGameBannerCommand(game.Id, stream));
        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.True(updated!.HasBanner);
    }

    [Fact]
    public async Task GetGameById_ReturnererSpillDetaljer()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var result = await new GetGameByIdQueryHandler(db).Handle(new GetGameByIdQuery(game.Id));
        Assert.True(result.IsSuccess);
        Assert.Equal("Spill", result.Value!.Name);
    }
}
