using FluentAssertions;
using TronderLeikan.Domain.Departments;

namespace TronderLeikan.Domain.Tests.Departments;

public class DepartmentTests
{
    [Fact]
    public void Create_SetsNameOgId()
    {
        var department = Department.Create("Teknologi");

        department.Id.Should().NotBeEmpty();
        department.Name.Should().Be("Teknologi");
    }

    [Fact]
    public void Rename_EndrerNavn()
    {
        var department = Department.Create("Gammelt navn");

        department.Rename("Nytt navn");

        department.Name.Should().Be("Nytt navn");
    }
}
