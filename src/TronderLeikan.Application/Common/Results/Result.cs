// src/TronderLeikan.Application/Common/Results/Result.cs
using TronderLeikan.Application.Common.Errors;

namespace TronderLeikan.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    // Implicit konvertering: Error → Result (void)
    public static implicit operator Result(Error error) => Failure(error);

    // Match for void-Result
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error!);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error) : base(false, error) { }

    // Implicit konvertering: T → Result<T>
    public static implicit operator Result<T>(T value) => new(value);

    // Implicit konvertering: Error → Result<T>
    public static implicit operator Result<T>(Error error) => new(error);

    // Match for Result<T>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
