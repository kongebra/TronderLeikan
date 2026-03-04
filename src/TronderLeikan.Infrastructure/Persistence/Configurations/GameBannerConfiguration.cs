using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class GameBannerConfiguration : IEntityTypeConfiguration<GameBanner>
{
    public void Configure(EntityTypeBuilder<GameBanner> builder)
    {
        // GameId er både PK og FK til Games — 1-til-1 relasjon
        builder.HasKey(b => b.GameId);
        builder.Property(b => b.ImageData).IsRequired();
        builder.Property(b => b.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("image/webp");

        builder.ToTable("GameBanners");
    }
}
