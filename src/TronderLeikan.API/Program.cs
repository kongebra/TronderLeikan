using Asp.Versioning;
using TronderLeikan.Application.Common;
using TronderLeikan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("TronderLeikan.Sender"))
    .WithMetrics(m => m.AddMeter("TronderLeikan.Sender"));
builder.Services.AddApplication();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

var connectionString = builder.Configuration.GetConnectionString("tronderleikan")
    ?? throw new InvalidOperationException("Connection string 'tronderleikan' ikke konfigurert.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseStatusCodePages();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();

// Gjør Program tilgjengelig for WebApplicationFactory i testprosjektet
public partial class Program { }
