using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Billing.Application.Commands.ApplyPromocode;
using Sapphire.Billing.Application.Commands.CreateTariff;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Application.Queries.GetTariffs;
using Sapphire.Billing.Application.Queries.GetWallet;
using Sapphire.Shared.Kernel.Common;

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
    public async Task<Result<TariffDto>> CreateTariff(CreateTariffCommand command)
        => await _mediator.Send(command);

    [HttpPost("wallets/{walletId}/promocodes")]
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
        => await _mediator.Send(new GetWalletQuery(userId));
}
