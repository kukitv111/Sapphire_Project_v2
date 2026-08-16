using System.Security.Claims;
using Sapphire.Auth.Application.Interfaces;

namespace Sapphire.Auth.Api.Services;

/// <summary>
/// Resolves the currently authenticated user from the JWT claims of the active HttpContext.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; }
    public string? Username { get; }
    public string? Email { get; }
    public IReadOnlyCollection<string> Roles { get; }
    public IReadOnlyCollection<string> Permissions { get; }
    public bool IsAuthenticated { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            Roles = [];
            Permissions = [];
            return;
        }

        UserId = Guid.TryParse(principal.FindFirstValue("sub"), out var id) ? id : null;
        Username = principal.FindFirstValue("username");
        Email = principal.FindFirstValue("email");
        Roles = principal.FindAll("role").Select(c => c.Value).ToArray();
        Permissions = principal.FindAll("permission").Select(c => c.Value).ToArray();
        IsAuthenticated = true;
    }
}
