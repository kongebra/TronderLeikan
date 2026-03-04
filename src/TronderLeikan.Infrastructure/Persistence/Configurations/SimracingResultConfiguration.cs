using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class SimracingResultConfiguration : IEntityTypeConfiguration<SimracingResult>
{
    public void Configure(EntityTypeBuilder<SimracingResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.GameId).IsRequired();
        builder.Property(r => r.PersonId).IsRequired();
        builder.Property(r => r.RaceTimeMs).IsRequired();

        // Indeks for rask henting av alle tider for et spill
        builder.HasIndex(r => r.GameId);
        // Unik per person per spill — én tid per løper
        builder.HasIndex(r => new { r.GameId, r.PersonId }).IsUnique();

        builder.ToTable("SimracingResults");
    }
}
