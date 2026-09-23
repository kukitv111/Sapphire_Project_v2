using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Queries.GetUserById;
using Sapphire.Auth.Application.Queries.GetUsers;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Api.Controllers;

/// <summary>
/// User management endpoints for the admin panel.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Sapphire.Shared.Security.PolicyNames.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<Result<IReadOnlyList<UserDto>>> GetUsers()
        => await _mediator.Send(new GetUsersQuery());

    [HttpGet("{userId:guid}")]
    public async Task<Result<UserDto>> GetUserById(Guid userId)
        => await _mediator.Send(new GetUserByIdQuery(userId));
}
