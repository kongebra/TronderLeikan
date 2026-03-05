using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.HasKey(p => p.EventId);
        builder.Property(p => p.ProcessedAt).IsRequired();
        builder.ToTable("ProcessedEvents");
    }
}
