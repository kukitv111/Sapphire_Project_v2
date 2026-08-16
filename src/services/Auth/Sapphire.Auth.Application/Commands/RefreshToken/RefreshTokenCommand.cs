using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Command to refresh authentication tokens.
/// </summary>
public sealed record RefreshTokenCommand : IRequest<Result<AuthResultDto>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
}
