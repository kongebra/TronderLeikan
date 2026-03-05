using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Infrastructure.Persistence.Outbox;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class EventStoreEntryConfiguration : IEntityTypeConfiguration<EventStoreEntry>
{
    public void Configure(EntityTypeBuilder<EventStoreEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StreamId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.StreamType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
        builder.ToTable("EventStore");
    }
}
