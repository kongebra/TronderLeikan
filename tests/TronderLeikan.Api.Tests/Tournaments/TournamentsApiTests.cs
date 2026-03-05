namespace TronderLeikan.Api.Tests.Tournaments;

[Collection(nameof(ApiTestCollection))]
public class TournamentsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_tournaments_returnerer_201_med_guid()
    {
        var slug = $"vm-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/v1/tournaments", new
        {
            name = "VM 2026",
            slug
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_tournament_by_slug_returnerer_turnering()
    {
        var slug = $"nm-{Guid.NewGuid():N}";
        var postResponse = await _client.PostAsJsonAsync("/api/v1/tournaments", new { name = "NM 2026", slug });
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync($"/api/v1/tournaments/{slug}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("NM 2026");
        body.GetProperty("slug").GetString().Should().Be(slug);
    }

    [Fact]
    public async Task GET_tournament_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync("/api/v1/tournaments/finnes-ikke");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Tournament.NotFound");
    }

    [Fact]
    public async Task PUT_point_rules_oppdaterer_poengregler()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Test", slug = $"test-{Guid.NewGuid():N}" }))
            .Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PutAsJsonAsync($"/api/v1/tournaments/{id}/point-rules", new
        {
            participation = 5,
            firstPlace = 15,
            secondPlace = 10,
            thirdPlace = 7,
            organizedWithParticipation = 3,
            organizedWithoutParticipation = 8,
            spectator = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_scoreboard_returnerer_tom_liste_uten_spill()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Scoreboard Test", slug = $"scoreboard-{Guid.NewGuid():N}" }))
            .Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync($"/api/v1/tournaments/{id}/scoreboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }
}
