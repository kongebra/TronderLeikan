using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests;

// Enkel InMemory-kontekst for Application-tester — ingen Infrastructure-avhengighet
internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SimracingResult> SimracingResults => Set<SimracingResult>();
    public DbSet<PersonImage> PersonImages => Set<PersonImage>();
    public DbSet<GameBanner> GameBanners => Set<GameBanner>();

    // Konfigurer modell for InMemory-databasen — speiler Infrastructure-konfigurasjonen
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bildentiteter bruker fremmednøkkel som primærnøkkel
        modelBuilder.Entity<PersonImage>().HasKey(p => p.PersonId);
        modelBuilder.Entity<GameBanner>().HasKey(b => b.GameId);

        // TournamentPointRules er et owned value object i Tournament
        modelBuilder.Entity<Tournament>().OwnsOne(t => t.PointRules);

        // Game bruker private backing fields for personlistene
        void ConfigureList(string fieldName)
        {
            modelBuilder.Entity<Game>()
                .Property<List<Guid>>(fieldName)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        ConfigureList("_participants");
        ConfigureList("_organizers");
        ConfigureList("_spectators");
        ConfigureList("_firstPlace");
        ConfigureList("_secondPlace");
        ConfigureList("_thirdPlace");
    }

    // Hjelpemetode for enkel oppsett i tester
    internal static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestAppDbContext(options);
    }
}
