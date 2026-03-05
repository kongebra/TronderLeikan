namespace TronderLeikan.Application.Persons.Commands.UpdatePerson;
public record UpdatePersonCommand(Guid PersonId, string FirstName, string LastName, Guid? DepartmentId);
