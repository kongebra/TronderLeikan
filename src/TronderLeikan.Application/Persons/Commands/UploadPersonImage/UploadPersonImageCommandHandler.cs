using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persistence.Images;

namespace TronderLeikan.Application.Persons.Commands.UploadPersonImage;

public sealed class UploadPersonImageCommandHandler(IAppDbContext db, IImageProcessor imageProcessor)
    : ICommandHandler<UploadPersonImageCommand>
{
    public async Task<Result> Handle(UploadPersonImageCommand command, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([command.PersonId], ct);
        if (person is null)
            return Result.Fail($"Person med Id {command.PersonId} finnes ikke.");

        var processedBytes = await imageProcessor.ProcessPersonImageAsync(command.ImageStream, ct);

        var existing = await db.PersonImages.FindAsync([command.PersonId], ct);
        if (existing is not null)
        {
            existing.ImageData = processedBytes;
        }
        else
        {
            db.PersonImages.Add(new PersonImage
            {
                PersonId = command.PersonId,
                ImageData = processedBytes,
                ContentType = "image/webp"
            });
        }

        person.SetProfileImage();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
