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
        // Sett med: dotnet user-secrets set "Zitadel:MasterKey" "<32-tegns-nøkkel>"
        var masterKey = builder.Configuration["Zitadel:MasterKey"]
            ?? throw new InvalidOperationException(
                "Zitadel:MasterKey er ikke konfigurert i user secrets. " +
                "Kjør: dotnet user-secrets set \"Zitadel:MasterKey\" \"<nøyaktig 32 tegn>\"");

        if (masterKey.Length != 32)
            throw new InvalidOperationException(
                $"Zitadel:MasterKey må være nøyaktig 32 tegn (er {masterKey.Length}).");

        // PostgreSQL-serveren som Zitadel-databasen bor på
        var postgresServer = database.Resource.Parent;

        // zitadel-api — Go-backend som håndterer OIDC, GRPC og admin-API
        // start-from-init oppretter schema og admin-bruker ved første oppstart
        var zitadelApi = builder
            .AddContainer($"{name}-api", "ghcr.io/zitadel/zitadel", "v4.11.0")
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithArgs("start-from-init", "--masterkey", masterKey)
            .WithEnvironment("ZITADEL_EXTERNALPORT", "8080")
            .WithEnvironment("ZITADEL_EXTERNALDOMAIN", "localhost")
            .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
            .WithEnvironment("ZITADEL_TLS_ENABLED", "false")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_USERNAME", "postgres")
            // IResourceBuilder<ParameterResource>-overload sikrer korrekt runtime-resolving av passordet
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_PASSWORD", postgresAdminPassword)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_USERNAME", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_PASSWORD", "zitadel")
            .WithEnvironment(ctx =>
            {
                // Host og port resolves av Aspire via EndpointProperty ved runtime
                var ep = postgresServer.PrimaryEndpoint;
                ctx.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_HOST"] =
                    ep.Property(EndpointProperty.Host);
                ctx.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_PORT"] =
                    ep.Property(EndpointProperty.Port);
            })
            // Bootstrap-mappe for PAT (Personal Access Token) — deles med zitadel-login
            .WithBindMount("./zitadel-bootstrap", "/zitadel/bootstrap")
            // Skriv login-client PAT til bootstrap-mappe ved første oppstart
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_LOGINCLIENTPATPATH", "/zitadel/bootstrap/login-client.pat")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_USERNAME", "login-client")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_NAME", "Automatically Initialized IAM_LOGIN_CLIENT")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_PAT_EXPIRATIONDATE", "2099-01-01T00:00:00Z")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_PASSWORDCHANGEREQUIRED", "false")
            // Login v2-URLer — peker på Traefik-proxyen (port 8080)
            .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_REQUIRED", "true")
            .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_BASEURI", "http://localhost:8080/ui/v2/login/")
            .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGINURLV2", "http://localhost:8080/ui/v2/login/login?authRequest=")
            .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGOUTURLV2", "http://localhost:8080/ui/v2/login/logout?post_logout_redirect=")
            .WaitFor(database);

        // zitadel-login — Next.js UI for innloggingsflyter (PathPrefix /ui/v2)
        var zitadelLogin = builder
            .AddContainer($"{name}-login", "ghcr.io/zitadel/zitadel-login", "v4.11.0")
            .WithHttpEndpoint(targetPort: 3000, name: "http")
            .WithEnvironment("ZITADEL_API_URL", zitadelApi.GetEndpoint("http"))
            .WithEnvironment("NEXT_PUBLIC_BASE_PATH", "/ui/v2/login")
            .WithEnvironment("CUSTOM_REQUEST_HEADERS", "Host:localhost,X-Forwarded-Proto:http")
            // Les login-client PAT fra bootstrap-mappen som zitadel-api skriver til
            .WithEnvironment("ZITADEL_SERVICE_USER_TOKEN_FILE", "/zitadel/bootstrap/login-client.pat")
            .WithBindMount("./zitadel-bootstrap", "/zitadel/bootstrap", isReadOnly: true)
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
