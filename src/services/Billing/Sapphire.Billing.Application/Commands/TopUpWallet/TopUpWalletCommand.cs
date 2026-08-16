using MediatR;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Billing.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Commands.TopUpWallet;

/// <summary>
/// Команда пополнения кошелька.
/// </summary>
public sealed record TopUpWalletCommand(Guid UserId, long AmountCents, string? Method = null, string? ReferenceId = null, string? Description = null) : IRequest<Result>;
