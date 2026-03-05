using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonQueryHandlerTests
{
    [Fact]
    public async Task GetPersons_ReturnererAllePersoner()
    {
        await using var db = TestAppDbContext.Create();
        db.Persons.AddRange(Person.Create("Ola", "Nordmann"), Person.Create("Kari", "Traa"));
        await db.SaveChangesAsync();
        var result = await new GetPersonsQueryHandler(db).Handle(new GetPersonsQuery());
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Length);
    }

    [Fact]
    public async Task GetPersonById_EksisterendePerson_ReturnererDetaljer()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(person.Id));
        Assert.True(result.IsSuccess);
        Assert.Equal("Ola", result.Value!.FirstName);
    }

    [Fact]
    public async Task GetPersonById_IkkeEksisterende_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
    }
}
