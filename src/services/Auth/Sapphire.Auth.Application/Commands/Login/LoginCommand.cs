using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.Login;

/// <summary>
/// Command to authenticate a user.
/// </summary>
public sealed record LoginCommand : IRequest<Result<AuthResultDto>>
{
    public string Login { get; init; } = string.Empty; // Username or Email
    public string Password { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
}
