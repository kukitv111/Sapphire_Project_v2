using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Queries.GetWallet;

/// <summary>
/// Запрос для получения текущего кошелька пользователя.
/// </summary>
public sealed record GetWalletQuery(Guid UserId) : IRequest<Result<WalletDto>>;
