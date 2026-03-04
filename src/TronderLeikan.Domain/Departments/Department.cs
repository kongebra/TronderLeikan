using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Departments;

public sealed class Department : Entity
{
    private Department() { }

    public string Name { get; private set; } = string.Empty;

    public static Department Create(string name) =>
        new() { Id = Guid.NewGuid(), Name = name };

    public void Rename(string name) => Name = name;
}
