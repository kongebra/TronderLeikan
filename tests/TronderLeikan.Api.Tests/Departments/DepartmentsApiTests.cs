namespace TronderLeikan.Api.Tests.Departments;

[Collection(nameof(ApiTestCollection))]
public class DepartmentsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_departments_returnerer_200_tom_liste()
    {
        var response = await _client.GetAsync("/api/v1/departments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task POST_departments_returnerer_201_med_guid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/departments", new
        {
            name = "IT-avdelingen"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_departments_med_tomt_navn_returnerer_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/departments", new
        {
            name = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Department.NameEmpty");
        body.GetProperty("status").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task Opprett_og_hent_department_happy_path()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/v1/departments", new { name = "Salg" });
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync("/api/v1/departments");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.EnumerateArray()
            .Select(d => d.GetProperty("name").GetString())
            .ToList();
        names.Should().Contain("Salg");
    }
}
