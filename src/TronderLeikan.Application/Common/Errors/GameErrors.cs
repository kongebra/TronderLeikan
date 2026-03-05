namespace TronderLeikan.Application.Common.Errors;
public static class GameErrors
{
    public static readonly Error NotFound           = Error.NotFound("Game.NotFound", "Spillet finnes ikke.");
    public static readonly Error AlreadyCompleted   = Error.Conflict("Game.AlreadyCompleted", "Spillet er allerede fullført.");
    public static readonly Error NoSimracingResults = Error.Validation("Game.NoSimracingResults", "Ingen racetider registrert for dette spillet.");
}
