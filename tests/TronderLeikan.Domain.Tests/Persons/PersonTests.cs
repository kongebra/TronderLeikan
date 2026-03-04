using FluentAssertions;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Domain.Tests.Persons;

public class PersonTests
{
    [Fact]
    public void Create_SetsNavnOgId()
    {
        var person = Person.Create("Ola", "Nordmann");

        person.Id.Should().NotBeEmpty();
        person.FirstName.Should().Be("Ola");
        person.LastName.Should().Be("Nordmann");
        person.DepartmentId.Should().BeNull();
        person.HasProfileImage.Should().BeFalse();
    }

    [Fact]
    public void Create_MedAvdeling_SetsDepartmentId()
    {
        var departmentId = Guid.NewGuid();

        var person = Person.Create("Kari", "Nordmann", departmentId);

        person.DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public void SetProfileImage_SetsHasProfileImageTilTrue()
    {
        var person = Person.Create("Ola", "Nordmann");

        person.SetProfileImage();

        person.HasProfileImage.Should().BeTrue();
    }

    [Fact]
    public void RemoveProfileImage_SetsHasProfileImageTilFalse()
    {
        var person = Person.Create("Ola", "Nordmann");
        person.SetProfileImage();

        person.RemoveProfileImage();

        person.HasProfileImage.Should().BeFalse();
    }

    [Fact]
    public void UpdateDepartment_EndrerAvdeling()
    {
        var person = Person.Create("Ola", "Nordmann");
        var nyAvdeling = Guid.NewGuid();

        person.UpdateDepartment(nyAvdeling);

        person.DepartmentId.Should().Be(nyAvdeling);
    }
}
