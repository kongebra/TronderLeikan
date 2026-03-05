using TronderLeikan.Application.Persons.Commands.CreatePerson;
using TronderLeikan.Application.Persons.Commands.UpdatePerson;
using TronderLeikan.Application.Persons.Commands.DeletePerson;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonCommandHandlerTests
{
    [Fact]
    public async Task CreatePerson_GyldigInput_LagrerPerson()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new CreatePersonCommandHandler(db).Handle(new CreatePersonCommand("Ola", "Nordmann", null));
        Assert.True(result.IsSuccess);
        Assert.Single(db.Persons.ToList());
    }

    [Fact]
    public async Task UpdatePerson_EksisterendePerson_OppdatererNavn()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new UpdatePersonCommandHandler(db).Handle(new UpdatePersonCommand(person.Id, "Kari", "Nordmann", null));
        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.Equal("Kari", updated!.FirstName);
    }

    [Fact]
    public async Task DeletePerson_EksisterendePerson_FjernerPerson()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new DeletePersonCommandHandler(db).Handle(new DeletePersonCommand(person.Id));
        Assert.True(result.IsSuccess);
        Assert.Empty(db.Persons.ToList());
    }

    [Fact]
    public async Task DeletePerson_IkkeEksisterende_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new DeletePersonCommandHandler(db).Handle(new DeletePersonCommand(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
    }
}
