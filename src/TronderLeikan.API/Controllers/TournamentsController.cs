using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Tournaments.Commands.CreateTournament;
using TronderLeikan.Application.Tournaments.Commands.UpdateTournamentPointRules;
using TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
using TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
using TronderLeikan.Application.Tournaments.Queries.GetTournaments;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.API.Controllers;

public sealed class TournamentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TournamentSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetTournamentsQuery(), ct)).Match(Ok, Problem);

    // Slug-basert oppslag — brukes av frontend for navigasjon
    [HttpGet("{slug}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetBySlug(string slug, CancellationToken ct) =>
        (await sender.Query(new GetTournamentBySlugQuery(slug), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTournamentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetBySlug), new { slug = command.Slug }, id),
            Problem);
    }

    [HttpPut("{id:guid}/point-rules")]
    public async Task<ActionResult> UpdatePointRules(
        Guid id, UpdateTournamentPointRulesCommand command, CancellationToken ct) =>
        (await sender.Send(command with { TournamentId = id }, ct))
            .Match<ActionResult>(() => NoContent(), Problem);

    [HttpGet("{id:guid}/scoreboard")]
    public async Task<ActionResult<ScoreboardEntryResponse[]>> GetScoreboard(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetScoreboardQuery(id), ct)).Match(Ok, Problem);
}
