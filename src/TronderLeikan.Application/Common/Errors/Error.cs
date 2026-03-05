namespace TronderLeikan.Application.Common.Errors;

public record Error(string Code, ErrorType Type, string Description)
{
    public static Error NotFound(string code, string description)       => new(code, ErrorType.NotFound, description);
    public static Error Validation(string code, string description)     => new(code, ErrorType.Validation, description);
    public static Error Conflict(string code, string description)       => new(code, ErrorType.Conflict, description);
    public static Error Forbidden(string code, string description)      => new(code, ErrorType.Forbidden, description);
    public static Error Unauthorized(string code, string description)   => new(code, ErrorType.Unauthorized, description);
    public static Error Unexpected(string code, string description)     => new(code, ErrorType.Unexpected, description);
}
