using FluentValidation;

namespace TronderLeikan.Application.Tournaments.Commands.CreateTournament;

public sealed class CreateTournamentCommandValidator : AbstractValidator<CreateTournamentCommand>
{
    public CreateTournamentCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug kan kun inneholde små bokstaver, tall og bindestrek.");
    }
}
