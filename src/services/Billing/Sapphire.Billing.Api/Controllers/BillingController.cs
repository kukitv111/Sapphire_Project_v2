using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Billing.Application.Commands.ApplyPromocode;
using Sapphire.Billing.Application.Commands.CreateTariff;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Application.Queries.GetTariffs;
using Sapphire.Billing.Application.Queries.GetWallet;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Security;

namespace Sapphire.Billing.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("tariffs")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<Result<TariffDto>> CreateTariff(CreateTariffCommand command)
        => await _mediator.Send(command);

    [HttpPost("wallets/{walletId}/promocodes")]
    [Authorize(Policy = PolicyNames.CashierOrAdmin)]
    public async Task<Result<WalletDto>> ApplyPromocode(Guid walletId, ApplyPromocodeCommand command)
    {
        command.WalletId = walletId;
        return await _mediator.Send(command);
    }

    [HttpGet("tariffs")]
    public async Task<Result<IReadOnlyList<TariffDto>>> GetTariffs()
        => await _mediator.Send(new GetTariffsQuery());

    [HttpGet("users/{userId:guid}/wallet")]
    public async Task<Result<WalletDto>> GetWallet(Guid userId)
    {
        var subject = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var staff = User.IsInRole("Cashier") || User.IsInRole("Admin") || User.IsInRole("Owner");
        if (!staff && (!Guid.TryParse(subject, out var callerId) || callerId != userId))
            return Result.Failure<WalletDto>(Error.Forbidden("Cannot access another user wallet"));
        return await _mediator.Send(new GetWalletQuery(userId));
    }
}
