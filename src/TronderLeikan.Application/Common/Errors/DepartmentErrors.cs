namespace TronderLeikan.Application.Common.Errors;
public static class DepartmentErrors
{
    public static readonly Error NotFound   = Error.NotFound("Department.NotFound", "Avdelingen finnes ikke.");
    public static readonly Error NameEmpty  = Error.Validation("Department.NameEmpty", "Navn kan ikke være tomt.");
}
