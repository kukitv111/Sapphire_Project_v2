using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Sapphire.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Auth.Application.Commands.ChangePassword;
using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Application.Commands.Register;
using Sapphire.Auth.Application.Commands.RefreshToken;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Application.Queries.GetCurrentUser;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [EnableRateLimiting(PolicyNames.AuthRateLimit)]
    public async Task<Result<AuthResultDto>> Register(RegisterCommand command)
        => await _mediator.Send(command);

    [HttpPost("login")]
    [EnableRateLimiting(PolicyNames.AuthRateLimit)]
    public async Task<Result<AuthResultDto>> Login(LoginCommand command)
        => await _mediator.Send(command);

    [HttpPost("refresh")]
    [EnableRateLimiting(PolicyNames.AuthRateLimit)]
    public async Task<Result<AuthResultDto>> RefreshToken(RefreshTokenCommand command)
        => await _mediator.Send(command);

    [HttpPost("change-password")]
    [Authorize]
    public async Task<Result> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICurrentUserService currentUser)
    {
        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authenticated user id is missing"));
        var command = new ChangePasswordCommand
        {
            UserId = currentUser.UserId ?? throw new InvalidOperationException("User not authenticated"),
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };
        return await _mediator.Send(command);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<Result<UserDto>> GetCurrentUser()
        => await _mediator.Send(new GetCurrentUserQuery());
}

/// <summary>
/// Request DTO for password change. UserId is taken from authenticated context, not from request body.
/// </summary>
public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
