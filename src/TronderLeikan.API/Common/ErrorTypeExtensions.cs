using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.API.Common;

public static class ErrorTypeExtensions
{
    public static int ToHttpStatus(this ErrorType type) => type switch
    {
        ErrorType.Validation          => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized        => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden           => StatusCodes.Status403Forbidden,
        ErrorType.NotFound            => StatusCodes.Status404NotFound,
        ErrorType.MethodNotAllowed    => StatusCodes.Status405MethodNotAllowed,
        ErrorType.Conflict            => StatusCodes.Status409Conflict,
        ErrorType.Gone                => StatusCodes.Status410Gone,
        ErrorType.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
        ErrorType.TooManyRequests     => StatusCodes.Status429TooManyRequests,
        ErrorType.ServiceUnavailable  => StatusCodes.Status503ServiceUnavailable,
        _                             => StatusCodes.Status500InternalServerError
    };
}
