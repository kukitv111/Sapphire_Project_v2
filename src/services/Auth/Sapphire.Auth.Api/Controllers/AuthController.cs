using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Auth.Application.Commands.ChangePassword;
using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Application.Commands.Register;
using Sapphire.Auth.Application.Commands.RefreshToken;
using Sapphire.Auth.Application.DTOs;
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
    public async Task<Result<AuthResultDto>> Register(RegisterCommand command)
        => await _mediator.Send(command);

    [HttpPost("login")]
    public async Task<Result<AuthResultDto>> Login(LoginCommand command)
        => await _mediator.Send(command);

    [HttpPost("refresh")]
    public async Task<Result<AuthResultDto>> RefreshToken(RefreshTokenCommand command)
        => await _mediator.Send(command);

    [HttpPost("change-password")]
    public async Task<Result> ChangePassword(ChangePasswordCommand command)
        => await _mediator.Send(command);
}
