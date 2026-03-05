using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.UpdatePerson;

public sealed class UpdatePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<UpdatePersonCommand>
{
    public async Task<Result> Handle(UpdatePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        person.Update(command.FirstName, command.LastName);
        person.UpdateDepartment(command.DepartmentId);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
