using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonById;

public sealed class GetPersonByIdQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> Handle(GetPersonByIdQuery query, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([query.PersonId], ct);
        if (person is null)
            return Result<PersonDetailResponse>.Fail($"Person med Id {query.PersonId} finnes ikke.");
        return Result<PersonDetailResponse>.Ok(
            new PersonDetailResponse(person.Id, person.FirstName, person.LastName, person.DepartmentId, person.HasProfileImage));
    }
}
