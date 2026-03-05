using System.Diagnostics;
using System.Diagnostics.Metrics;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Common.Behaviors;

// Tracing og metrics for alle commands og queries — synlig i Aspire Dashboard
public sealed class ObservabilityBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    // Delt ActivitySource og Meter — registreres i AddServiceDefaults
    internal static readonly ActivitySource ActivitySource = new("TronderLeikan.Sender");
    internal static readonly Meter Meter = new("TronderLeikan.Sender");

    // Antall dispatchede requests, tagget med type og resultat
    private static readonly Counter<long> RequestCounter =
        Meter.CreateCounter<long>("sender.requests.total", description: "Antall dispatchede commands og queries");

    // Latency-histogram — grunnlag for P95/P99 alerts
    private static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("sender.requests.duration", "ms", description: "Varighet per request-type");

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag("sender.request", name);

        TResponse response = default!;
        Exception? caughtException = null;
        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        finally
        {
            sw.Stop();

            // Kansellering er ikke en feil — registreres separat uten Error-status på span
            var isCancelled = caughtException is OperationCanceledException && ct.IsCancellationRequested;
            var isException = caughtException is not null && !isCancelled;
            var resultInterface = response as IResult;
            var isFailure = isException || resultInterface is { IsSuccess: false };
            var errorCode = isException
                ? caughtException!.GetType().Name
                : resultInterface?.Error?.Code;

            if (isFailure)
            {
                activity?.SetStatus(ActivityStatusCode.Error, errorCode);
                activity?.SetTag("sender.error", errorCode);
            }

            // Registrer exception-detaljer på span i henhold til OTel semantiske konvensjoner (kun ved feil, ikke kansellering)
            if (isException)
                activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type",       caughtException!.GetType().FullName },
                    { "exception.message",    caughtException!.Message },
                    { "exception.stacktrace", caughtException!.StackTrace ?? string.Empty }
                }));

            var resultLabel = isCancelled ? "cancelled" : isFailure ? "failure" : "success";
            var tags = new TagList
            {
                { "request.type",   name },
                { "request.result", resultLabel }
            };
            if (errorCode is not null)
                tags.Add("request.error_code", errorCode);

            RequestCounter.Add(1, tags);
            RequestDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
        }

        // Rethrow etter at metrics er registrert
        if (caughtException is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caughtException).Throw();

        return response;
    }
}
