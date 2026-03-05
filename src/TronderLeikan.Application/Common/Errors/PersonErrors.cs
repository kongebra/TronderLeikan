namespace TronderLeikan.Application.Common.Errors;
public static class PersonErrors
{
    public static readonly Error NotFound = Error.NotFound("Person.NotFound", "Personen finnes ikke.");
}
