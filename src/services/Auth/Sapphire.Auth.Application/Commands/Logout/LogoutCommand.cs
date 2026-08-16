using MediatR;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.Logout;

/// <summary>
/// Command to logout a user (revoke refresh token).
/// </summary>
public sealed record LogoutCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string? RefreshToken { get; init; }
    public bool RevokeAll { get; init; }
}
