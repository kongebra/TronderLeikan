using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Infrastructure.Services;

// Produksjonsimplementasjon av IDateTimeProvider
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
