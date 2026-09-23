using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Application.Queries.GetEntitlements;
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
    public async Task<Result<EntitlementDto>> AssignTariff(Guid userId, AssignTariffToUserCommand command)
    {
        command.UserId = userId;
        var key = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key))
            return Result.Failure<EntitlementDto>(Error.Validation("Idempotency-Key header is required"));
        command.IdempotencyKey = key;
        return await _mediator.Send(command);
    }

    [HttpGet]
    public async Task<Result<IReadOnlyList<EntitlementDto>>> GetEntitlements(Guid userId)
    {
        var subject = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var staff = User.IsInRole("Cashier") || User.IsInRole("Admin") || User.IsInRole("Owner");
        if (!staff && (!Guid.TryParse(subject, out var callerId) || callerId != userId))
            return Result.Failure<IReadOnlyList<EntitlementDto>>(Error.Forbidden("Cannot access another user's entitlements"));
        return await _mediator.Send(new GetEntitlementsQuery(userId));
    }
}
