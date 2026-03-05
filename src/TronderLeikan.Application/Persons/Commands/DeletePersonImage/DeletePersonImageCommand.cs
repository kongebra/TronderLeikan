using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Persons.Commands.DeletePersonImage;
public record DeletePersonImageCommand(Guid PersonId) : ICommand;
