using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Commands.AddOrganizer;
using TronderLeikan.Application.Games.Commands.AddParticipant;
using TronderLeikan.Application.Games.Commands.AddSpectator;
using TronderLeikan.Application.Games.Commands.CompleteGame;
using TronderLeikan.Application.Games.Commands.CompleteSimracingGame;
using TronderLeikan.Application.Games.Commands.CreateGame;
using TronderLeikan.Application.Games.Commands.RegisterSimracingResult;
using TronderLeikan.Application.Games.Commands.UpdateGame;
using TronderLeikan.Application.Games.Commands.UploadGameBanner;
using TronderLeikan.Application.Games.Queries.GetGameById;
using TronderLeikan.Application.Games.Queries.GetSimracingResults;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class GamesController(ISender sender) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GameDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetGameByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateGameCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateGameCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult> AddParticipant(Guid id, AddParticipantCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/organizers")]
    public async Task<ActionResult> AddOrganizer(Guid id, AddOrganizerCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/spectators")]
    public async Task<ActionResult> AddSpectator(Guid id, AddSpectatorCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CompleteGameCommand command, CancellationToken ct) =>
        (await sender.Send(command with { GameId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Banner-opplasting
    [HttpPut("{id:guid}/banner")]
    public async Task<ActionResult> UploadBanner(Guid id, IFormFile banner, CancellationToken ct)
    {
        await using var ms = await ToMemoryStreamAsync(banner, ct);
        var result = await sender.Send(new UploadGameBannerCommand(id, ms), ct);
        return result.Match<ActionResult>(() => NoContent(), Problem);
    }

    [HttpGet("{id:guid}/simracing-results")]
    public async Task<ActionResult<SimracingResultResponse[]>> GetSimracingResults(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetSimracingResultsQuery(id), ct)).Match(Ok, Problem);

    [HttpPost("{id:guid}/simracing-results")]
    public async Task<ActionResult<Guid>> RegisterSimracingResult(
        Guid id, RegisterSimracingResultCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { GameId = id }, ct);
        return result.Match(
            resultId => CreatedAtAction(nameof(GetSimracingResults), new { id }, resultId),
            Problem);
    }

    [HttpPost("{id:guid}/simracing-results/complete")]
    public async Task<ActionResult> CompleteSimracing(Guid id, CancellationToken ct) =>
        (await sender.Send(new CompleteSimracingGameCommand(id), ct))
            .Match<ActionResult>(() => NoContent(), Problem);
}
