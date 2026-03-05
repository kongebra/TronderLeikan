namespace TronderLeikan.Api.Tests.Games;

[Collection(nameof(ApiTestCollection))]
public class GamesApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Hjelper — oppretter turnering og returnerer id
    private async Task<Guid> OpprettTurnering() =>
        await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Test", slug = $"t-{Guid.NewGuid():N}" }))
            .Content.ReadFromJsonAsync<Guid>();

    // Hjelper — oppretter person og returnerer id
    private async Task<Guid> OpprettPerson(string fornavn, string etternavn) =>
        await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = fornavn, lastName = etternavn }))
            .Content.ReadFromJsonAsync<Guid>();

    [Fact]
    public async Task POST_games_returnerer_201_med_guid()
    {
        var tournamentId = await OpprettTurnering();

        var response = await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Testspill",
            gameType = 0 // Standard
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_game_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync($"/api/v1/games/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Game.NotFound");
    }

    [Fact]
    public async Task POST_participants_og_complete_game_happy_path()
    {
        var tournamentId = await OpprettTurnering();
        var personId1 = await OpprettPerson("Per", "Testesen");
        var personId2 = await OpprettPerson("Pål", "Testesen");
        var personId3 = await OpprettPerson("Espen", "Testesen");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Finalespill",
            gameType = 0
        })).Content.ReadFromJsonAsync<Guid>();

        // Legg til deltakere
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId1 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId2 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants",
            new { gameId, personId = personId3 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fullfør spill
        var completeResponse = await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/complete", new
        {
            gameId,
            firstPlace  = new[] { personId1 },
            secondPlace = new[] { personId2 },
            thirdPlace  = new[] { personId3 }
        });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifiser at spillet er done
        var body = await (await _client.GetAsync($"/api/v1/games/{gameId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isDone").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task POST_simracing_results_og_complete_beregner_plasseringer()
    {
        var tournamentId = await OpprettTurnering();
        var p1 = await OpprettPerson("Rask", "Raser");
        var p2 = await OpprettPerson("Midt", "Raser");
        var p3 = await OpprettPerson("Treg", "Raser");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId,
            name = "Simracing 1",
            gameType = 1 // Simracing
        })).Content.ReadFromJsonAsync<Guid>();

        // Registrer racetider (lavest er best)
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p1, raceTimeMs = 90000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p2, raceTimeMs = 95000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p3, raceTimeMs = 100000L });

        // Fullfør automatisk
        var completeResponse = await _client.PostAsync(
            $"/api/v1/games/{gameId}/simracing-results/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifiser at spillet er done
        var body = await (await _client.GetAsync($"/api/v1/games/{gameId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isDone").GetBoolean().Should().BeTrue();

        // Verifiser plasseringer
        var firstPlace = body.GetProperty("firstPlace").EnumerateArray()
            .Select(e => e.GetGuid()).ToList();
        firstPlace.Should().Contain(p1);
    }

    [Fact]
    public async Task GET_simracing_results_returnerer_sortert_liste()
    {
        var tournamentId = await OpprettTurnering();
        var p1 = await OpprettPerson("Rask2", "Raser");
        var p2 = await OpprettPerson("Treg2", "Raser");

        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games", new
        {
            tournamentId, name = "Simracing 2", gameType = 1
        })).Content.ReadFromJsonAsync<Guid>();

        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p2, raceTimeMs = 100000L });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/simracing-results",
            new { gameId, personId = p1, raceTimeMs = 90000L });

        var response = await _client.GetAsync($"/api/v1/games/{gameId}/simracing-results");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var times = body.EnumerateArray()
            .Select(r => r.GetProperty("raceTimeMs").GetInt64()).ToList();
        times.Should().BeInAscendingOrder();
    }
}
