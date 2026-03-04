namespace TronderLeikan.Application.Common.Interfaces;

// Testbar klokke — unngår spredte DateTime.UtcNow-kall
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
