using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePerson;

public sealed class DeletePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonCommand>
{
    public async Task<Result> Handle(DeletePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");
        db.Persons.Remove(person);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
