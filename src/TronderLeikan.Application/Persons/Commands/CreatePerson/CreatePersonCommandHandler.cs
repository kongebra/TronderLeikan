using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Persons.Commands.CreatePerson;

public sealed class CreatePersonCommandHandler(IAppDbContext db)
    : ICommandHandler<CreatePersonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePersonCommand command, CancellationToken ct = default)
    {
        var person = Person.Create(command.FirstName, command.LastName, command.DepartmentId);
        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);
        return person.Id;
    }
}
