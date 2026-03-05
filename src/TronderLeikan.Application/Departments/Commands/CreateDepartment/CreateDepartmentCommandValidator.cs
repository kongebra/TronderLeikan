using FluentValidation;

namespace TronderLeikan.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithErrorCode("Department.NameEmpty")
                .WithMessage("Avdelingsnavn kan ikke være tomt.")
            .MaximumLength(200);
    }
}
