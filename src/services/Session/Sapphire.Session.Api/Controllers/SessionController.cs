using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Session.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<Result<SessionDto>> StartSession(StartSessionCommand command)
    {
        var currentUserId = ResolveCurrentUserId();
        if (currentUserId is null)
            return Result.Failure<SessionDto>(Error.Unauthorized("Authenticated user id claim is missing or invalid"));

        var authenticatedCommand = command with { UserId = currentUserId.Value };
        return await _mediator.Send(authenticatedCommand);
    }

    private Guid? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
