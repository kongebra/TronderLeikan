using TronderLeikan.Application.Departments.Commands.CreateDepartment;

namespace TronderLeikan.Application.Tests.Departments;

public sealed class CreateDepartmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_GyldigNavn_ReturnererNyId()
    {
        await using var db = TestAppDbContext.Create();
        var handler = new CreateDepartmentCommandHandler(db);
        var result = await handler.Handle(new CreateDepartmentCommand("IT"));
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        var dept = await db.Departments.FindAsync(result.Value);
        Assert.NotNull(dept);
        Assert.Equal("IT", dept.Name);
    }

    [Fact]
    public async Task Handle_TomtNavn_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var handler = new CreateDepartmentCommandHandler(db);
        var result = await handler.Handle(new CreateDepartmentCommand(""));
        Assert.False(result.IsSuccess);
    }
}
