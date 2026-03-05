namespace TronderLeikan.Application.Common.Errors;

public enum ErrorType
{
    Validation,           // 400
    Unauthorized,         // 401
    Forbidden,            // 403
    NotFound,             // 404
    MethodNotAllowed,     // 405
    Conflict,             // 409
    Gone,                 // 410
    UnprocessableEntity,  // 422
    TooManyRequests,      // 429
    Unexpected,           // 500
    ServiceUnavailable    // 503
}
