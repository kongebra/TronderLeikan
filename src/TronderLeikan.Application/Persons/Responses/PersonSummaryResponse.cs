namespace TronderLeikan.Application.Persons.Responses;
public record PersonSummaryResponse(Guid Id, string FirstName, string LastName, Guid? DepartmentId, bool HasProfileImage);
