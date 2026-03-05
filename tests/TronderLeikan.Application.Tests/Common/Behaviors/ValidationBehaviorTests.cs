using FluentValidation;
using TronderLeikan.Application.Common.Behaviors;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Tests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    private record TestCommand(string Name);

    private sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator() =>
            RuleFor(c => c.Name).NotEmpty().WithMessage("Navn er påkrevd.");
    }

    [Fact]
    public async Task Handle_GyldigRequest_KallerNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>(
            [new TestValidator()]);
        var nextKalt = false;
        var expectedId = Guid.NewGuid();

        var result = await behavior.Handle(
            new TestCommand("Gyldig navn"),
            () => { nextKalt = true; return Task.FromResult<Result<Guid>>(expectedId); },
            CancellationToken.None);

        nextKalt.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task Handle_UgyldigRequest_ReturnererValidationError()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>(
            [new TestValidator()]);

        var result = await behavior.Handle(
            new TestCommand(""),
            () => throw new Exception("next skal ikke kalles"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Contain("Navn er påkrevd.");
    }

    [Fact]
    public async Task Handle_IngenValidators_KallerNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>([]);
        var nextKalt = false;

        await behavior.Handle(
            new TestCommand(""),
            () => { nextKalt = true; return Task.FromResult<Result<Guid>>(Guid.NewGuid()); },
            CancellationToken.None);

        nextKalt.Should().BeTrue();
    }
}
