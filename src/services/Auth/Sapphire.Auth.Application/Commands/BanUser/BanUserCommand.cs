using MediatR;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.BanUser;

/// <summary>
/// Command to ban a user.
/// </summary>
public sealed record BanUserCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid BannedBy { get; init; }
}
