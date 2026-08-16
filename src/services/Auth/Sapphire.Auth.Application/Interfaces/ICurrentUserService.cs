namespace Sapphire.Auth.Application.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// Implemented by API layer using HttpContext.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
}
