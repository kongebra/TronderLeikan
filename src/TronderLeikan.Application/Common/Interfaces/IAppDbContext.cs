using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Person> Persons { get; }
    DbSet<Tournament> Tournaments { get; }
    DbSet<Game> Games { get; }
    DbSet<SimracingResult> SimracingResults { get; }
    DbSet<PersonImage> PersonImages { get; }
    DbSet<GameBanner> GameBanners { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
