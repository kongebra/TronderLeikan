namespace TronderLeikan.Application.Persons.Dtos;
public record PersonSummaryDto(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
