namespace TronderLeikan.Application.Persons.Responses;
public record PersonDetailResponse(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
