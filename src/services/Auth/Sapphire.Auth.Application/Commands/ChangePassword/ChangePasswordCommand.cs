using MediatR;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.ChangePassword;

/// <summary>
/// Command to change user password.
/// </summary>
public sealed record ChangePasswordCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
