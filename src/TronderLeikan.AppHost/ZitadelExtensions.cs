// Aspire-extension for å kjøre full Zitadel v4-stack lokalt
// Spinner opp: zitadel-api (Go), zitadel-login (Next.js) og Traefik som proxy

using Aspire.Hosting;

internal static class ZitadelExtensions
{
    /// <summary>
    /// Legger til Zitadel v4-stack (api + login + Traefik proxy) i Aspire-applikasjonen.
    /// Returnerer Traefik-ressursen som er det felles inngangspunktet (port 8080).
    /// </summary>
    internal static IResourceBuilder<ContainerResource> AddZitadel(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<ParameterResource> postgresAdminPassword)
    {
        // Masterkey hentes fra user secrets — må være nøyaktig 32 tegn
        var masterKey = builder.Configuration["Zitadel:MasterKey"]
            ?? "MasterkeyNeedsToHave32Chars!!";

        // PostgreSQL-serveren som Zitadel-databasen bor på
        var postgresServer = database.Resource.Parent;

        // zitadel-api — Go-backend som håndterer OIDC, GRPC og admin-API
        // start-from-init oppretter schema og admin-bruker ved første oppstart
        var zitadelApi = builder
            .AddContainer($"{name}-api", "ghcr.io/zitadel/zitadel", "v4.11.0")
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithArgs("start-from-init", "--masterkey", masterKey)
            .WithEnvironment("ZITADEL_EXTERNALPORT", "8080")
            .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
            .WithEnvironment("ZITADEL_DOMAIN", "localhost")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_USERNAME", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_SSL_MODE", "disable")
            // IResourceBuilder<ParameterResource>-overload sikrer korrekt runtime-resolving av passordet
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_PASSWORD", postgresAdminPassword)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_USERNAME", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_PASSWORD", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_SSL_MODE", "disable")
            .WithEnvironment(ctx =>
            {
                // Host og port resolves av Aspire via EndpointProperty ved runtime
                var ep = postgresServer.PrimaryEndpoint;
                ctx.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_HOST"] =
                    ep.Property(EndpointProperty.Host);
                ctx.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_PORT"] =
                    ep.Property(EndpointProperty.Port);
            })
            // Bootstrap-mappe for initial admin PAT (Personal Access Token)
            .WithBindMount("./zitadel-bootstrap", "/app/bootstrap")
            .WaitFor(database);

        // zitadel-login — Next.js UI for innloggingsflyter (PathPrefix /ui/v2)
        // MERK: Bildets navn må verifiseres mot Zitadel v4 releases
        var zitadelLogin = builder
            .AddContainer($"{name}-login", "ghcr.io/zitadel/zitadel-login", "v4.11.0")
            .WithHttpEndpoint(targetPort: 3000, name: "http")
            .WithEnvironment("ZITADEL_API_URL", zitadelApi.GetEndpoint("http"))
            .WaitFor(zitadelApi);

        // Traefik — felles inngangspunkt som ruter til riktig backend
        // Konfigurasjonen leses fra ./traefik/ (bind-mountet som read-only)
        var traefik = builder
            .AddContainer($"{name}-proxy", "traefik", "v3.6.8")
            .WithHttpEndpoint(port: 8080, targetPort: 80, name: "http")
            .WithBindMount("./traefik", "/etc/traefik", isReadOnly: true)
            .WithEnvironment("ZITADEL_API_INTERNAL_URL", zitadelApi.GetEndpoint("http"))
            .WithEnvironment("ZITADEL_LOGIN_INTERNAL_URL", zitadelLogin.GetEndpoint("http"))
            .WaitFor(zitadelApi)
            .WaitFor(zitadelLogin);

        return traefik;
    }
}
