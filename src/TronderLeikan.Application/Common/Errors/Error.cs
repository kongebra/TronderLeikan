namespace TronderLeikan.Application.Common.Errors;

public record Error(string Code, ErrorType Type, string Description)
{
    public static Error NotFound(string code, string desc)       => new(code, ErrorType.NotFound, desc);
    public static Error Validation(string code, string desc)     => new(code, ErrorType.Validation, desc);
    public static Error Conflict(string code, string desc)       => new(code, ErrorType.Conflict, desc);
    public static Error Forbidden(string code, string desc)      => new(code, ErrorType.Forbidden, desc);
    public static Error Unauthorized(string code, string desc)   => new(code, ErrorType.Unauthorized, desc);
    public static Error Unexpected(string code, string desc)     => new(code, ErrorType.Unexpected, desc);
}
