using MediatR;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Application.Commands.Register;

/// <summary>
/// Command to register a new user.
/// </summary>
public sealed record RegisterCommand : IRequest<Result<AuthResultDto>>
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid? BranchId { get; init; }
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
}
