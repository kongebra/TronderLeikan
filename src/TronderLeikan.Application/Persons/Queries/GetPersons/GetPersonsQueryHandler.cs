using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersons;

public sealed class GetPersonsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonsQuery, PersonSummaryResponse[]>
{
    public async Task<Result<PersonSummaryResponse[]>> Handle(GetPersonsQuery query, CancellationToken ct = default)
    {
        var persons = await db.Persons
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PersonSummaryResponse(p.Id, p.FirstName, p.LastName, p.DepartmentId, p.HasProfileImage))
            .ToArrayAsync(ct);
        return persons;
    }
}
