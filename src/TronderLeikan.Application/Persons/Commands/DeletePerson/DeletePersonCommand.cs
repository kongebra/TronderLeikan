using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Persons.Commands.DeletePerson;
public record DeletePersonCommand(Guid PersonId) : ICommand;
