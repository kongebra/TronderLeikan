using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Games.Commands.UploadGameBanner;
public record UploadGameBannerCommand(Guid GameId, Stream BannerStream) : ICommand;
