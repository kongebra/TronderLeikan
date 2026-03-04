using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class PersonImageConfiguration : IEntityTypeConfiguration<PersonImage>
{
    public void Configure(EntityTypeBuilder<PersonImage> builder)
    {
        // PersonId er både PK og FK til Persons — 1-til-1 relasjon
        builder.HasKey(i => i.PersonId);
        builder.Property(i => i.ImageData).IsRequired();
        builder.Property(i => i.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("image/webp");

        builder.ToTable("PersonImages");
    }
}
