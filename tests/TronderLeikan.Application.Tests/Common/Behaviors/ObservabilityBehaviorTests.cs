using System.Collections.Concurrent;
using System.Diagnostics;
using TronderLeikan.Application.Common.Behaviors;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Tests.Common.Behaviors;

public sealed class ObservabilityBehaviorTests : IDisposable
{
    // ConcurrentBag — ActivityStopped kan kalles fra ulike tråder
    private readonly ConcurrentBag<Activity> _recordedActivities = new();
    private readonly ActivityListener _listener;

    public ObservabilityBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "TronderLeikan.Sender",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private record TestQuery(string Term);

    [Fact]
    public async Task Handle_SuccessResult_StarterOgStopperSpanUtenFeil()
    {
        var behavior = new ObservabilityBehavior<TestQuery, Result<string>>();

        await behavior.Handle(
            new TestQuery("søk"),
            () => Task.FromResult<Result<string>>("treff"),
            CancellationToken.None);

        var activity = _recordedActivities.Should().ContainSingle(a => a.DisplayName == "TestQuery").Which;
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Handle_FailureResult_SetterFeilstatus()
    {
        var behavior = new ObservabilityBehavior<TestQuery, Result<string>>();
        var error = Error.NotFound("Test.NotFound", "Ikke funnet");

        await behavior.Handle(
            new TestQuery("søk"),
            () => Task.FromResult<Result<string>>(error),
            CancellationToken.None);

        var activity = _recordedActivities.Should().ContainSingle().Which;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem("sender.error").Should().Be("Test.NotFound");
    }

    public void Dispose() => _listener.Dispose();
}
