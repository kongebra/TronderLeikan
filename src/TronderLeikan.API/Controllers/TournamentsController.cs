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

public sealed class TournamentsController(
    ICommandHandler<CreateTournamentCommand, Guid> createHandler,
    ICommandHandler<UpdateTournamentPointRulesCommand> pointRulesHandler,
    IQueryHandler<GetTournamentsQuery, TournamentSummaryResponse[]> getTournamentsHandler,
    IQueryHandler<GetTournamentBySlugQuery, TournamentDetailResponse> getBySlugHandler,
    IQueryHandler<GetScoreboardQuery, ScoreboardEntryResponse[]> scoreboardHandler)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TournamentSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await getTournamentsHandler.Handle(new GetTournamentsQuery(), ct)).Match(Ok, Problem);

    // Slug-basert oppslag — brukes av frontend for navigasjon
    [HttpGet("{slug}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetBySlug(string slug, CancellationToken ct) =>
        (await getBySlugHandler.Handle(new GetTournamentBySlugQuery(slug), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTournamentCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetBySlug), new { slug = command.Slug }, id),
            Problem);
    }

    [HttpPut("{id:guid}/point-rules")]
    public async Task<ActionResult> UpdatePointRules(
        Guid id, UpdateTournamentPointRulesCommand command, CancellationToken ct) =>
        (await pointRulesHandler.Handle(command with { TournamentId = id }, ct))
            .Match<ActionResult>(() => NoContent(), Problem);

    [HttpGet("{id:guid}/scoreboard")]
    public async Task<ActionResult<ScoreboardEntryResponse[]>> GetScoreboard(Guid id, CancellationToken ct) =>
        (await scoreboardHandler.Handle(new GetScoreboardQuery(id), ct)).Match(Ok, Problem);
}
