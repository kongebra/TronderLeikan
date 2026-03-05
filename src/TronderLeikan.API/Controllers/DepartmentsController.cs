using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Departments.Commands.CreateDepartment;
using TronderLeikan.Application.Departments.Queries.GetDepartments;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.API.Controllers;

// Kontroller for avdelingsressurser — GET og POST
public sealed class DepartmentsController(
    ICommandHandler<CreateDepartmentCommand, Guid> createHandler,
    IQueryHandler<GetDepartmentsQuery, DepartmentResponse[]> getHandler)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DepartmentResponse[]>> GetAll(CancellationToken ct) =>
        (await getHandler.Handle(new GetDepartmentsQuery(), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetAll), id),
            Problem);
    }
}
