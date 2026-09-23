using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Entities;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Billing.Application.Commands.AssignTariffToUser;

public sealed record AssignTariffToUserCommand : IRequest<Result<EntitlementDto>>
{
    public Guid UserId { get; set; }
    public Guid TariffId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed record EntitlementDto(Guid Id, Guid UserId, Guid TariffId, Guid PaymentTransactionId,
    long PurchasedCents, long AvailableCents, string IdempotencyKey);

public sealed class AssignTariffToUserCommandHandler : IRequestHandler<AssignTariffToUserCommand, Result<EntitlementDto>>
{
    private readonly ITariffRepository _tariffs;
    private readonly IWalletRepository _wallets;
    private readonly IEntitlementRepository _entitlements;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTariffToUserCommandHandler(ITariffRepository tariffs, IWalletRepository wallets,
        IEntitlementRepository entitlements, IUnitOfWork unitOfWork)
    {
        _tariffs = tariffs;
        _wallets = wallets;
        _entitlements = entitlements;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EntitlementDto>> Handle(AssignTariffToUserCommand request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty || request.TariffId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
            return Result.Failure<EntitlementDto>(Error.Validation("User, tariff and idempotency key are required"));

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var existing = await _entitlements.GetByIdempotencyKeyAsync(request.UserId, request.IdempotencyKey, ct);
            if (existing is not null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return existing.TariffId == request.TariffId
                    ? Result.Success(ToDto(existing))
                    : Result.Failure<EntitlementDto>(Error.Conflict("Idempotency key belongs to another tariff"));
            }
            var tariff = await _tariffs.GetByIdAsync(request.TariffId, ct);
            if (tariff is null || !tariff.IsActive)
                return await Failure(Error.NotFound("Active tariff not found"), ct);

            // TODO: External card/cash provider integration. This purchase only spends existing wallet funds.
            var wallet = await _wallets.GetByUserIdAsync(request.UserId, ct);
            if (wallet is null)
                return await Failure(Error.NotFound("Wallet not found"), ct);
            var price = tariff.Type switch
            {
                TariffType.PerMinute => checked(tariff.PricePerMinuteCents * 60),
                TariffType.PerHour => tariff.PricePerHourCents,
                TariffType.Package => tariff.PackagePriceCents,
                _ => 0
            };
            if (price <= 0) return await Failure(Error.Validation("Tariff price is missing"), ct);
            var debit = wallet.Debit(Money.FromCents(price), $"tariff:{request.TariffId}");
            if (debit.IsFailure)
                return await Failure(Error.Conflict("Insufficient wallet balance"), ct);

            var payment = WalletTransaction.Create(wallet.Id, WalletTransactionType.Purchase,
                price, 0, wallet.MainBalance.Cents, wallet.BonusBalance.Cents,
                "wallet", request.IdempotencyKey, "Tariff purchase");
            var entitlement = new TariffEntitlement(request.UserId, tariff, payment.Id, request.IdempotencyKey);
            await _entitlements.AddAsync(entitlement, payment, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return Result.Success(ToDto(entitlement));
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            // A concurrent request with the same key may have committed first.
            // Read after rollback so a retry returns the original purchase.
            try
            {
                var committed = await _entitlements.GetByIdempotencyKeyAsync(
                    request.UserId, request.IdempotencyKey, ct);
                if (committed is not null)
                    return committed.TariffId == request.TariffId
                        ? Result.Success(ToDto(committed))
                        : Result.Failure<EntitlementDto>(Error.Conflict("Idempotency key belongs to another tariff"));
            }
            catch { /* Preserve the original write failure below. */ }
            throw;
        }
    }

    private async Task<Result<EntitlementDto>> Failure(Error error, CancellationToken ct)
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        return Result.Failure<EntitlementDto>(error);
    }

    private static EntitlementDto ToDto(TariffEntitlement e) => new(e.Id, e.UserId, e.TariffId,
        e.PaymentTransactionId, e.PurchasedCents, e.AvailableCents, e.IdempotencyKey);
}
