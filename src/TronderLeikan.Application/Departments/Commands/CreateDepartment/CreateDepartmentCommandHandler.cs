using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Departments;

namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandHandler(IAppDbContext db)
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Guid>.Fail("Navn kan ikke være tomt.");

        var department = Department.Create(command.Name);
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(department.Id);
    }
}
