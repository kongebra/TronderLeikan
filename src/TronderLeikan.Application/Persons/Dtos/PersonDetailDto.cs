namespace TronderLeikan.Application.Persons.Dtos;
public record PersonDetailDto(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
