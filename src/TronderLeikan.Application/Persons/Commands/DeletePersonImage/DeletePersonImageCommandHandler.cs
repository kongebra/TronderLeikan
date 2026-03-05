using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Persons.Commands.DeletePersonImage;

public sealed class DeletePersonImageCommandHandler(IAppDbContext db)
    : ICommandHandler<DeletePersonImageCommand>
{
    public async Task<Result> Handle(DeletePersonImageCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return PersonErrors.NotFound;

        var image = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (image is not null)
            db.PersonImages.Remove(image);

        person.RemoveProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
