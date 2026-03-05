using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePerson;

public sealed class DeletePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonCommand>
{
    public async Task<Result> Handle(DeletePersonCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;
        db.Persons.Remove(person);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
