using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Api.Controllers;

[ApiController]
[Route("api/billing/users/{userId}/tariffs")]
public class UserTariffController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserTariffController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<Result<WalletDto>> AssignTariff(Guid userId, AssignTariffToUserCommand command)
    {
        command.UserId = userId;
        return await _mediator.Send(command);
    }
}
