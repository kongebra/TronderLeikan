using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.Application.Departments.Queries.GetDepartments;
public record GetDepartmentsQuery : IQuery<DepartmentResponse[]>;
