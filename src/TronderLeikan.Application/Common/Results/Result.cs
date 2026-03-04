namespace TronderLeikan.Application.Common.Results;

// Ikke-generisk Result for kommandoer uten returverdi
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool success, string? error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

// Generisk Result for kommandoer og queries med returverdi
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) { Value = value; }
    private Result(string error) : base(false, error) { }

    public static Result<T> Ok(T value) => new(value);
    public new static Result<T> Fail(string error) => new(error);
}
