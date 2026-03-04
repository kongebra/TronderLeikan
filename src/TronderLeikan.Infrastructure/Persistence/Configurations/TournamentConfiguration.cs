using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(200);
        builder.HasIndex(t => t.Slug).IsUnique();

        // TournamentPointRules er et owned value object — flates ut i Tournament-tabellen
        // Alle poengregler lagres som kolonner direkte på Tournaments-tabellen
        builder.OwnsOne(t => t.PointRules, pr =>
        {
            pr.Property(p => p.Participation)
                .HasColumnName("PointRules_Participation")
                .HasDefaultValue(3);
            pr.Property(p => p.FirstPlace)
                .HasColumnName("PointRules_FirstPlace")
                .HasDefaultValue(3);
            pr.Property(p => p.SecondPlace)
                .HasColumnName("PointRules_SecondPlace")
                .HasDefaultValue(2);
            pr.Property(p => p.ThirdPlace)
                .HasColumnName("PointRules_ThirdPlace")
                .HasDefaultValue(1);
            pr.Property(p => p.OrganizedWithParticipation)
                .HasColumnName("PointRules_OrgWithParticipation")
                .HasDefaultValue(1);
            pr.Property(p => p.OrganizedWithoutParticipation)
                .HasColumnName("PointRules_OrgWithoutParticipation")
                .HasDefaultValue(3);
            pr.Property(p => p.Spectator)
                .HasColumnName("PointRules_Spectator")
                .HasDefaultValue(1);
        });

        builder.ToTable("Tournaments");
    }
}
