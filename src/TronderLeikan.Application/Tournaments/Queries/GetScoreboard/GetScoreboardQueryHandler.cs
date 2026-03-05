using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;

public sealed class GetScoreboardQueryHandler(IAppDbContext db)
    : IQueryHandler<GetScoreboardQuery, ScoreboardEntryResponse[]>
{
    public async Task<Result<ScoreboardEntryResponse[]>> Handle(GetScoreboardQuery query, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([query.TournamentId], ct);
        if (tournament is null)
            return Result<ScoreboardEntryResponse[]>.Fail($"Turnering med Id {query.TournamentId} finnes ikke.");

        var rules = tournament.PointRules;

        // Hent alle fullførte spill i turneringen
        var games = await db.Games
            .Where(g => g.TournamentId == query.TournamentId && g.IsDone)
            .ToListAsync(ct);

        var allPersonIds = games.SelectMany(g =>
            g.Participants.Concat(g.Organizers).Concat(g.Spectators)).Distinct().ToList();

        var persons = await db.Persons
            .Where(p => allPersonIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Akkumuler poeng per person
        var points = new Dictionary<Guid, int>();

        foreach (var game in games)
        {
            // Deltakere får deltakerpoeng
            foreach (var personId in game.Participants)
                points[personId] = points.GetValueOrDefault(personId) + rules.Participation;

            // Arrangører — deltakelse avhenger av IsOrganizersParticipating
            foreach (var personId in game.Organizers)
            {
                if (game.IsOrganizersParticipating)
                    points[personId] = points.GetValueOrDefault(personId) + rules.OrganizedWithParticipation + rules.Participation;
                else
                    points[personId] = points.GetValueOrDefault(personId) + rules.OrganizedWithoutParticipation;
            }

            // Tilskuere
            foreach (var personId in game.Spectators)
                points[personId] = points.GetValueOrDefault(personId) + rules.Spectator;

            // Plasseringer er additive oppå deltakerpoeng
            foreach (var personId in game.FirstPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.FirstPlace;
            foreach (var personId in game.SecondPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.SecondPlace;
            foreach (var personId in game.ThirdPlace)
                points[personId] = points.GetValueOrDefault(personId) + rules.ThirdPlace;
        }

        // Sorter synkende, beregn rank med ties
        var sorted = points.OrderByDescending(kv => kv.Value).ToList();
        var entries = new List<ScoreboardEntryResponse>();
        var rank = 1;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (i > 0 && sorted[i].Value < sorted[i - 1].Value)
                rank = i + 1;
            var personId = sorted[i].Key;
            if (!persons.TryGetValue(personId, out var person))
                continue;
            entries.Add(new ScoreboardEntryResponse(personId, person.FirstName, person.LastName, sorted[i].Value, rank));
        }

        return Result<ScoreboardEntryResponse[]>.Ok([.. entries]);
    }
}
