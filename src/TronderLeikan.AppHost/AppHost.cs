var builder = DistributedApplication.CreateBuilder(args);

// Eksplisitt passord-parameter — samme verdi brukes av postgres og Zitadel
// Sett i user secrets: dotnet user-secrets set "Parameters:postgres-password" "<passord>"
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

// PostgreSQL — database for TrønderLeikan og Zitadel på samme instans
// md5-autentisering brukes istedenfor scram-sha-256 fordi Zitadels Go pgx-driver
// ikke støtter scram-sha-256-plus (channel binding) mot Aspires SSL-konfigurerte postgres
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "md5")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=md5 --auth-local=md5")
    // NB: Endre volum-navn (eller slett eksisterende volum) ved bytte av auth-oppsett,
    // ellers vil initdb-innstillingene over ikke kjøres på allerede initialisert data.
    .WithDataVolume("tronderleikan-postgres-data-md5")
    .WithLifetime(ContainerLifetime.Persistent);
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
