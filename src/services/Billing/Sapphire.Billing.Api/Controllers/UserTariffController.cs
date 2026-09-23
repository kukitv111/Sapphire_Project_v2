using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Security;

namespace Sapphire.Billing.Api.Controllers;

[ApiController]
[Route("api/billing/users/{userId}/tariffs")]
[Authorize]
public class UserTariffController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserTariffController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CashierOrAdmin)]
    public async Task<Result<WalletDto>> AssignTariff(Guid userId, AssignTariffToUserCommand command)
    {
        command.UserId = userId;
        return await _mediator.Send(command);
    }
}
