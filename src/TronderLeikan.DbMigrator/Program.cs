using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;
using TronderLeikan.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

// Aspire ServiceDefaults gir helse-sjekk og telemetri
builder.AddServiceDefaults();

// IDateTimeProvider kreves av AppDbContext — må registreres før DbContext
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Npgsql-kobling via Aspire connection string "tronderleikan"
builder.AddNpgsqlDbContext<AppDbContext>("tronderleikan");

using var host = builder.Build();
await host.StartAsync();

// Kjør alle ventende migrations mot databasen og avslutt
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();

Console.WriteLine("Migrations fullfort.");
await host.StopAsync();
