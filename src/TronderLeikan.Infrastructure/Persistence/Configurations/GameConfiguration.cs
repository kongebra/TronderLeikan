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
        builder.Property(g => g.Location).HasMaxLength(500);
        builder.Property(g => g.IsDone);
        builder.Property(g => g.GameType);
        builder.Property(g => g.IsOrganizersParticipating);
        builder.Property(g => g.HasBanner);

        builder.HasIndex(g => g.TournamentId);

        // Game lagres med alle personlister som native PostgreSQL uuid[]-kolonner.
        // Backing fields er private List<Guid> i Game-entiteten.
        // Npgsql mapper List<Guid> automatisk til uuid[] i PostgreSQL.
        void UuidArray(string felt, string kolonne) =>
            builder.Property<List<Guid>>(felt)
                .HasColumnName(kolonne)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasDefaultValueSql("'{}'::uuid[]");

        UuidArray("_participants", "Participants");
        UuidArray("_organizers",   "Organizers");
        UuidArray("_spectators",   "Spectators");
        UuidArray("_firstPlace",   "FirstPlace");
        UuidArray("_secondPlace",  "SecondPlace");
        UuidArray("_thirdPlace",   "ThirdPlace");

        builder.ToTable("Games");
    }
}
