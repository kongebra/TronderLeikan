var builder = DistributedApplication.CreateBuilder(args);

// Eksplisitt passord-parameter — samme verdi brukes av postgres og Zitadel
// Sett i user secrets: dotnet user-secrets set "Parameters:postgres-password" "<passord>"
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

// PostgreSQL — database for TrønderLeikan og Zitadel på samme instans
var postgres = builder.AddPostgres("postgres", password: postgresPassword);
var tronderleikanDb = postgres.AddDatabase("tronderleikan");

// Zitadel bruker en separat database på samme Postgres-instans
var zitadelDb = postgres.AddDatabase("zitadel");

// Zitadel v4-stack: api + login UI + Traefik proxy
// Traefik eksponeres på port 8080 som eneste inngangspunkt
var zitadel = builder.AddZitadel("zitadel", zitadelDb, postgresPassword);

// DbMigrator kjøres automatisk ved oppstart, etter at PostgreSQL er klar
// API venter til migrations er fullfort
var migrator = builder.AddProject<Projects.TronderLeikan_DbMigrator>("migrator")
    .WithReference(tronderleikanDb)
    .WaitFor(tronderleikanDb);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithReference(tronderleikanDb)
    // Eksponerer Zitadel-endepunktet som services__zitadel-proxy__http__0
    .WithReference(zitadel.GetEndpoint("http"))
    .WaitFor(migrator)
    .WaitFor(zitadel)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
