using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Persons;

public sealed class Person : Entity
{
    private Person() { }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public bool HasProfileImage { get; private set; }

    public static Person Create(string firstName, string lastName, Guid? departmentId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DepartmentId = departmentId
        };

    public void SetProfileImage() => HasProfileImage = true;
    public void RemoveProfileImage() => HasProfileImage = false;
    public void UpdateDepartment(Guid? departmentId) => DepartmentId = departmentId;
}
