namespace TronderLeikan.Application.Common.Errors;
public static class TournamentErrors
{
    public static readonly Error NotFound  = Error.NotFound("Tournament.NotFound", "Turneringen finnes ikke.");
    public static readonly Error SlugTaken = Error.Conflict("Tournament.SlugTaken", "Slug er allerede i bruk.");
}
