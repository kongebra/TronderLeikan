using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.API.Common;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    // Overload som tar vår Error-type — bruker ProblemDetailsFactory (RFC 9457)
    protected ObjectResult Problem(Error error) =>
        Problem(
            detail: error.Description,
            title: error.Code,
            statusCode: error.Type.ToHttpStatus());
}
