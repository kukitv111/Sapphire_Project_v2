using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Session.Application.Commands.FinishSession;
using Sapphire.Session.Application.DTOs;
using Sapphire.Session.Application.Queries.GetSessions;
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

    [HttpGet]
    [Authorize(Policy = Sapphire.Shared.Security.PolicyNames.CashierOrAdmin)]
    public async Task<Result<IReadOnlyList<SessionDto>>> GetSessions([FromQuery] int limit = 50)
        => await _mediator.Send(new GetSessionsQuery(Math.Clamp(limit, 1, 200)));

    [HttpPost("{sessionId:guid}/complete")]
    public Task<Result<SessionDto>> Complete(Guid sessionId)
        => Finish(sessionId, cancel: false);

    [HttpPost("{sessionId:guid}/cancel")]
    [Authorize(Policy = Sapphire.Shared.Security.PolicyNames.CashierOrAdmin)]
    public Task<Result<SessionDto>> Cancel(Guid sessionId)
        => Finish(sessionId, cancel: true);

    private Task<Result<SessionDto>> Finish(Guid sessionId, bool cancel)
    {
        var actor = ResolveCurrentUserId();
        if (actor is null)
            return Task.FromResult(Result.Failure<SessionDto>(Error.Unauthorized("User id claim is missing")));
        var staff = User.IsInRole("Cashier") || User.IsInRole("Admin") || User.IsInRole("Owner");
        return _mediator.Send(new FinishSessionCommand(sessionId, actor.Value, staff, cancel));
    }

    private Guid? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
