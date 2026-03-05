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

        var response = await next();
        sw.Stop();

        // Inspiser resultatet via IResult — begge Result-typer implementerer dette
        var resultInterface = response as IResult;
        var isFailure = resultInterface is { IsSuccess: false };
        var errorCode = isFailure ? resultInterface!.Error?.Code : null;

        if (isFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorCode);
            activity?.SetTag("sender.error", errorCode);
        }

        var tags = new TagList
        {
            { "request.type",   name },
            { "request.result", isFailure ? "failure" : "success" }
        };
        if (errorCode is not null)
            tags.Add("request.error_code", errorCode);

        RequestCounter.Add(1, tags);
        RequestDuration.Record(sw.Elapsed.TotalMilliseconds, tags);

        return response;
    }
}
