namespace TronderLeikan.Application.Persons.Commands.CreatePerson;
public record CreatePersonCommand(string FirstName, string LastName, Guid? DepartmentId);
