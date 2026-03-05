using Microsoft.Extensions.DependencyInjection;
using TronderLeikan.Application.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;

namespace TronderLeikan.Application.Tests.Common;

public sealed class SenderTests
{
    // ── Fake typer for testing ──────────────────────────────────────────────

    private sealed record FakeCommand : ICommand<string>;
    private sealed record FakeVoidCommand : ICommand;
    private sealed record FakeQuery : IQuery<string>;

    private sealed class FakeCommandHandler : ICommandHandler<FakeCommand, string>
    {
        public Task<Result<string>> Handle(FakeCommand command, CancellationToken ct) =>
            Task.FromResult<Result<string>>("ok");
    }

    private sealed class FakeVoidCommandHandler : ICommandHandler<FakeVoidCommand>
    {
        public Task<Result> Handle(FakeVoidCommand command, CancellationToken ct) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeQueryHandler : IQueryHandler<FakeQuery, string>
    {
        public Task<Result<string>> Handle(FakeQuery query, CancellationToken ct) =>
            Task.FromResult<Result<string>>("resultat");
    }

    // Kaster synkront — tester TargetInvocationException-unwrapping
    private sealed class ThrowingHandler : ICommandHandler<FakeCommand, string>
    {
        public Task<Result<string>> Handle(FakeCommand command, CancellationToken ct) =>
            throw new InvalidOperationException("original melding");
    }

    // Behavior som logger rekkefølge for pipeline-test
    private sealed class TrackingBehavior<TRequest, TResponse>(string name, List<string> log)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add(name);
            return await next();
        }
    }

    // ── Hjelpemetode ────────────────────────────────────────────────────────

    private static Sender Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return new Sender(services.BuildServiceProvider());
    }

    // ── Handler-resolusjon ──────────────────────────────────────────────────

    [Fact]
    public async Task Send_Command_DispatcherTilKorrektHandler()
    {
        var sender = Build(s => s.AddScoped<ICommandHandler<FakeCommand, string>, FakeCommandHandler>());

        var result = await sender.Send(new FakeCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Send_VoidCommand_DispatcherTilKorrektHandler()
    {
        var sender = Build(s => s.AddScoped<ICommandHandler<FakeVoidCommand>, FakeVoidCommandHandler>());

        var result = await sender.Send(new FakeVoidCommand());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Query_DispatcherTilKorrektHandler()
    {
        var sender = Build(s => s.AddScoped<IQueryHandler<FakeQuery, string>, FakeQueryHandler>());

        var result = await sender.Query(new FakeQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("resultat");
    }

    [Fact]
    public async Task Send_HandlerMangler_ReturnererUnexpectedError()
    {
        var sender = Build(_ => { });

        var result = await sender.Send(new FakeCommand());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Sender.HandlerNotFound");
    }

    // ── Pipeline-rekkefølge ─────────────────────────────────────────────────

    [Fact]
    public async Task Send_BehaviorsFørstRegistrertErYtterst()
    {
        var log = new List<string>();

        var sender = Build(s =>
        {
            s.AddScoped<ICommandHandler<FakeCommand, string>, FakeCommandHandler>();
            // Første registrert = ytterst = kjøres først
            s.AddScoped<IPipelineBehavior<FakeCommand, Result<string>>>(
                _ => new TrackingBehavior<FakeCommand, Result<string>>("første", log));
            s.AddScoped<IPipelineBehavior<FakeCommand, Result<string>>>(
                _ => new TrackingBehavior<FakeCommand, Result<string>>("andre", log));
        });

        await sender.Send(new FakeCommand());

        log.Should().Equal("første", "andre");
    }

    // ── Exception-håndtering ────────────────────────────────────────────────

    [Fact]
    public async Task Send_HandlerKasterSynkront_UnwrapperTargetInvocationException()
    {
        var sender = Build(s => s.AddScoped<ICommandHandler<FakeCommand, string>, ThrowingHandler>());

        // Skal kaste InvalidOperationException — IKKE TargetInvocationException
        var act = () => sender.Send(new FakeCommand());

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("original melding");
    }
}
