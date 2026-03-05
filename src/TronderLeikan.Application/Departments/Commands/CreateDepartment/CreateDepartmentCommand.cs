using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;
public record CreateDepartmentCommand(string Name) : ICommand<Guid>;
