namespace TronderLeikan.Application.Common.Interfaces;

// Stub for fremtidig autentisering (Zitadel/Entra ID)
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
