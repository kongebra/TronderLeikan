using FluentValidation;

namespace TronderLeikan.Application.Persons.Commands.CreatePerson;

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(200);
    }
}
