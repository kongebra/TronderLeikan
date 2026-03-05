using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Commands.UploadPersonImage;
using TronderLeikan.Application.Persons.Commands.DeletePersonImage;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonImageCommandHandlerTests
{
    private sealed class FakeImageProcessor : IImageProcessor
    {
        public Task<byte[]> ProcessPersonImageAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(new byte[] { 1, 2, 3 });
        public Task<byte[]> ProcessGameBannerAsync(Stream input, CancellationToken ct) =>
            Task.FromResult(new byte[] { 4, 5, 6 });
    }

    [Fact]
    public async Task UploadPersonImage_LagrerBildeOgSetterFlag()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
        var result = await new UploadPersonImageCommandHandler(db, new FakeImageProcessor())
            .Handle(new UploadPersonImageCommand(person.Id, stream));

        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.True(updated!.HasProfileImage);
        var image = await db.PersonImages.FindAsync(person.Id);
        Assert.NotNull(image);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.ImageData);
    }

    [Fact]
    public async Task DeletePersonImage_FjernerBildeOgClearerFlag()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        person.SetProfileImage();
        db.Persons.Add(person);
        db.PersonImages.Add(new PersonImage { PersonId = person.Id, ImageData = [1], ContentType = "image/webp" });
        await db.SaveChangesAsync();

        var result = await new DeletePersonImageCommandHandler(db).Handle(new DeletePersonImageCommand(person.Id));

        Assert.True(result.IsSuccess);
        var updated = await db.Persons.FindAsync(person.Id);
        Assert.False(updated!.HasProfileImage);
    }
}
