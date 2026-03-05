// Testcontainers-basert Zitadel-fixture for integrasjonstester.
// Spinner opp zitadel-api + PostgreSQL i et isolert Docker-nettverk.
// Brukes via [Collection(nameof(ZitadelCollection))] på testklasser.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;

namespace TronderLeikan.Infrastructure.Tests.Zitadel;

public sealed class ZitadelFixture : IAsyncLifetime
{
    // Masterkey for lokal testing — ikke bruk i produksjon
    private const string MasterKey = "MasterkeyNeedsToHave32Chars!!";
    private const string ZitadelImage = "ghcr.io/zitadel/zitadel:v4.11.0";

    private readonly INetwork _network;
    private readonly PostgreSqlContainer _postgres;
    private readonly IContainer _zitadelApi;

    /// <summary>Basis-URL til Zitadel API (f.eks. http://localhost:12345).</summary>
    public string ZitadelBaseUrl { get; private set; } = default!;

    /// <summary>Initial admin PAT lest fra bootstrap-filen etter oppstart.</summary>
    public string InitialAdminPat { get; private set; } = default!;

    public ZitadelFixture()
    {
        // Isolert Docker-nettverk sørger for at containerne kan snakke med hverandre
        _network = new NetworkBuilder().Build();

        _postgres = new PostgreSqlBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            // "postgres" er standard-databasen. Zitadel oppretter "zitadel"-databasen selv
            // ved start-from-init via admin-brukerens superuser-privilegier — ingen pre-oppretting nødvendig.
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _zitadelApi = new ContainerBuilder()
            .WithImage(ZitadelImage)
            .WithNetwork(_network)
            .WithCommand("start-from-init", "--masterkey", MasterKey)
            // Tilfeldig host-port for å unngå konflikter ved parallelle testruns.
            // MERK: ZITADEL_EXTERNALPORT=8080 refererer til den interne porten som Zitadel
            // bruker for å generere OIDC-metadata og redirect-URIer. For API-tester (PAT-autentisering)
            // er ikke ekstern port relevant. For OAuth-flow-tester trengs fast port-binding.
            .WithPortBinding(8080, true)
            .WithEnvironment("ZITADEL_EXTERNALPORT", "8080")
            .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
            .WithEnvironment("ZITADEL_DOMAIN", "localhost")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_HOST", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_PORT", "5432")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_USERNAME", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_PASSWORD", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_ADMINUSER_SSL_MODE", "disable")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_USERNAME", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_PASSWORD", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_APPUSER_SSL_MODE", "disable")
            // Zitadel er klar når /debug/healthz returnerer 200
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req
                    .ForPath("/debug/healthz")
                    .ForPort(8080)))
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start nettverk og PostgreSQL før Zitadel
        await _network.CreateAsync();
        await _postgres.StartAsync();
        await _zitadelApi.StartAsync();

        var port = _zitadelApi.GetMappedPublicPort(8080);
        ZitadelBaseUrl = $"http://localhost:{port}";

        // Les initial admin PAT fra bootstrap-filen som Zitadel skriver ved init.
        // Filstien er /app/bootstrap/zitadel-admin-sa.pat inne i containeren.
        // TODO: Verifiser eksakt filsti mot Zitadel v4.11.0 release notes.
        var patBytes = await _zitadelApi.ReadFileAsync("/app/bootstrap/zitadel-admin-sa.pat");
        InitialAdminPat = System.Text.Encoding.UTF8.GetString(patBytes).Trim();
    }

    public async Task DisposeAsync()
    {
        await _zitadelApi.DisposeAsync();
        await _postgres.DisposeAsync();
        await _network.DeleteAsync();
    }
}

/// <summary>
/// xUnit collection definition som deler ZitadelFixture på tvers av testklasser.
/// Merke testklasser med [Collection(nameof(ZitadelCollection))].
/// </summary>
[CollectionDefinition(nameof(ZitadelCollection))]
public class ZitadelCollection : ICollectionFixture<ZitadelFixture> { }
