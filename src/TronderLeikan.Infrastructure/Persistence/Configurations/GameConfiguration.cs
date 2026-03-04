using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.TournamentId).IsRequired();
        builder.Property(g => g.Name).IsRequired().HasMaxLength(500);
        builder.Property(g => g.Description).HasMaxLength(5000);
        builder.Property(g => g.IsDone);
        builder.Property(g => g.GameType);
        builder.Property(g => g.IsOrganizersParticipating);
        builder.Property(g => g.HasBanner);

        builder.HasIndex(g => g.TournamentId);

        // Game lagres med alle personlister som native PostgreSQL uuid[]-kolonner.
        // Backing fields er private List<Guid> i Game-entiteten.
        // Npgsql mapper List<Guid> automatisk til uuid[] i PostgreSQL.
        // PropertyAccessMode.Field forteller EF Core å lese/skrive direkte mot feltet, ikke property.

        builder.Property<List<Guid>>("_participants")
            .HasColumnName("Participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_organizers")
            .HasColumnName("Organizers")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_spectators")
            .HasColumnName("Spectators")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_firstPlace")
            .HasColumnName("FirstPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_secondPlace")
            .HasColumnName("SecondPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.Property<List<Guid>>("_thirdPlace")
            .HasColumnName("ThirdPlace")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasDefaultValueSql("'{}'::uuid[]");

        builder.ToTable("Games");
    }
}
