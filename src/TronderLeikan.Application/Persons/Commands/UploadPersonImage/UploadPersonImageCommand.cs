using TronderLeikan.Application.Common.Interfaces;

namespace TronderLeikan.Application.Persons.Commands.UploadPersonImage;
public record UploadPersonImageCommand(Guid PersonId, Stream ImageStream) : ICommand;
