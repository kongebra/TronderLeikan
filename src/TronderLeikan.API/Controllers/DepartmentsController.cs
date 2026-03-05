using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Departments.Commands.CreateDepartment;
using TronderLeikan.Application.Departments.Queries.GetDepartments;
using TronderLeikan.Application.Departments.Responses;

namespace TronderLeikan.API.Controllers;

// Kontroller for avdelingsressurser — GET og POST
public sealed class DepartmentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DepartmentResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetDepartmentsQuery(), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetAll), null, id),
            Problem);
    }
}
