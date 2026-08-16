using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Session.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<Result<SessionDto>> StartSession(StartSessionCommand command)
        => await _mediator.Send(command);
}
