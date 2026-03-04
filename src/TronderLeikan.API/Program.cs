using TronderLeikan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Kobling til PostgreSQL via Aspire — connection string hentes fra environment
var connectionString = builder.Configuration.GetConnectionString("tronderleikan")
    ?? throw new InvalidOperationException("Connection string 'tronderleikan' ikke konfigurert.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.Run();
