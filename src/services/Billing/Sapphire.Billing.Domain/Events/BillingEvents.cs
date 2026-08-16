using Sapphire.Billing.Domain.Enums;
using Sapphire.Shared.Kernel.Events;

namespace Sapphire.Billing.Domain.Events;

/// <summary>
/// Published when a wallet is created for a user.
/// </summary>
public sealed record WalletCreatedEvent : DomainEventBase
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Published when funds are added to the wallet (top-up or refund).
/// </summary>
public sealed record WalletDepositedEvent : DomainEventBase
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public long AmountCents { get; init; }
    public string? Method { get; init; }
    public string? ReferenceId { get; init; }
    public long BalanceAfterCents { get; init; }
}

/// <summary>
/// Published when funds leave the wallet (purchase, session charge).
/// </summary>
public sealed record WalletDebitedEvent : DomainEventBase
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public long AmountCents { get; init; }
    public long MainAmountCents { get; init; }
    public long BonusAmountCents { get; init; }
    public string? ReferenceId { get; init; }
    public long BalanceAfterCents { get; init; }
}

/// <summary>
/// Published when the bonus balance is credited.
/// </summary>
public sealed record WalletBonusCreditedEvent : DomainEventBase
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public long BonusAmountCents { get; init; }
    public string? ReferenceId { get; init; }
    public long BonusBalanceAfterCents { get; init; }
}

/// <summary>
/// Published when the bonus balance is debited.
/// </summary>
public sealed record WalletBonusDebitedEvent : DomainEventBase
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public long BonusAmountCents { get; init; }
    public string? ReferenceId { get; init; }
    public long BonusBalanceAfterCents { get; init; }
}

/// <summary>
/// Published when a tariff is created.
/// </summary>
public sealed record TariffCreatedEvent : DomainEventBase
{
    public Guid TariffId { get; init; }
    public string Name { get; init; } = string.Empty;
    public TariffType Type { get; init; }
}

/// <summary>
/// Published when a promocode is created.
/// </summary>
public sealed record PromocodeCreatedEvent : DomainEventBase
{
    public Guid PromocodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public PromocodeType Type { get; init; }
}

/// <summary>
/// Published when a promocode is successfully redeemed.
/// </summary>
public sealed record PromocodeAppliedEvent : DomainEventBase
{
    public Guid PromocodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public long PurchaseAmountCents { get; init; }
    public long DiscountedAmountCents { get; init; }
}
