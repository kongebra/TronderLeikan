using FluentValidation;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Behaviors;

// Kjører FluentValidation-validators automatisk — returnerer Error.Validation ved brudd
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Materialisér én gang — unngår dobbel iterasjon av IEnumerable
        var validatorList = validators.ToList();
        if (validatorList.Count == 0)
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, ct)));
        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Bruk feilkoden fra første brudd — gir domenespesifikk feilkode i respons
        var error = Error.Validation(
            code: failures[0].ErrorCode ?? "Validation.Failed",
            description: string.Join("; ", failures.Select(f => f.ErrorMessage)));

        // dynamic løser implicit operator (Error → Result / Result<T>) ved runtime
        return (TResponse)(dynamic)error;
    }
}
