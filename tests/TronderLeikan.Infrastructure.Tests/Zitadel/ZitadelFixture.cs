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
    // Masterkey for lokal testing — må være nøyaktig 32 tegn, ikke bruk i produksjon
    private const string MasterKey = "MasterkeyNeedsToHave32Chars!!!!!";
    private const string ZitadelImage = "ghcr.io/zitadel/zitadel:v4.11.0";

    private readonly INetwork _network;
    private readonly PostgreSqlContainer _postgres;
    private readonly IContainer _zitadelApi;

    /// <summary>Basis-URL til Zitadel API (f.eks. http://localhost:12345).</summary>
    public string ZitadelBaseUrl { get; private set; } = default!;

    /// <summary>Login-klient PAT lest fra bootstrap-filen etter oppstart.</summary>
    public string LoginClientPat { get; private set; } = default!;

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
            .WithEnvironment("ZITADEL_EXTERNALDOMAIN", "localhost")
            .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
            .WithEnvironment("ZITADEL_TLS_ENABLED", "false")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_HOST", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_PORT", "5432")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_USERNAME", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_PASSWORD", "postgres")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_USERNAME", "zitadel")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_PASSWORD", "zitadel")
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

        // Les initial login-klient PAT fra bootstrap-filen som Zitadel skriver ved init.
        // Filstien styres av ZITADEL_FIRSTINSTANCE_LOGINCLIENTPATPATH.
        // Zitadel kan skrive filen litt etter at /debug/healthz er grønt — vi prøver et par ganger.
        var patPath = "/zitadel/bootstrap/login-client.pat";
        byte[] patBytes = [];
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                patBytes = await _zitadelApi.ReadFileAsync(patPath);
                if (patBytes.Length > 0) break;
            }
            catch
            {
                // Filen finnes ikke ennå — vent og prøv igjen
            }
            await Task.Delay(TimeSpan.FromSeconds(attempt));
        }
        LoginClientPat = System.Text.Encoding.UTF8.GetString(patBytes).Trim();
        if (string.IsNullOrWhiteSpace(LoginClientPat))
            throw new InvalidOperationException(
                $"Zitadel-fixture: PAT-filen '{patPath}' var tom eller utilgjengelig etter 10 forsøk. " +
                "Sjekk at ZITADEL_FIRSTINSTANCE_LOGINCLIENTPATPATH er riktig og at containeren startet uten feil.");
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
