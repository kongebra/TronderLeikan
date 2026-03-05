using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.Application.Departments.Queries.GetDepartments;

public sealed class GetDepartmentsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetDepartmentsQuery, DepartmentResponse[]>
{
    public async Task<Result<DepartmentResponse[]>> Handle(GetDepartmentsQuery query, CancellationToken ct = default)
    {
        var departments = await db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name))
            .ToArrayAsync(ct);
        return Result<DepartmentResponse[]>.Ok(departments);
    }
}
