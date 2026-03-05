namespace TronderLeikan.Api.Tests.Persons;

[Collection(nameof(ApiTestCollection))]
public class PersonsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_persons_returnerer_201_med_guid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Ola",
            lastName = "Nordmann"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_person_som_ikke_finnes_returnerer_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/v1/persons/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Person.NotFound");
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("detail").GetString().Should().Be("Personen finnes ikke.");
    }

    [Fact]
    public async Task Opprett_og_hent_person_happy_path()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Kari",
            lastName = "Nordmann",
            departmentId = (Guid?)null
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/persons/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Kari");
        body.GetProperty("lastName").GetString().Should().Be("Nordmann");
        body.GetProperty("hasProfileImage").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PUT_person_oppdaterer_navn()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Gammel", lastName = "Navn" }))
            .Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/persons/{id}", new
        {
            personId = id,
            firstName = "Nytt",
            lastName = "Navn"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = await (await _client.GetAsync($"/api/v1/persons/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Nytt");
    }

    [Fact]
    public async Task DELETE_person_returnerer_204()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Slett", lastName = "Meg" }))
            .Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/persons/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_person_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.DeleteAsync($"/api/v1/persons/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_persons_returnerer_liste()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/v1/persons", new { firstName = "Liste", lastName = "Test" });
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var newId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync("/api/v1/persons");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.EnumerateArray()
            .Select(p => p.GetProperty("id").GetGuid())
            .ToList();
        ids.Should().Contain(newId);
    }
}
