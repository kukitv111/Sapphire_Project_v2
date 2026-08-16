using Sapphire.Shared.Kernel.Exceptions;

namespace Sapphire.Billing.Domain.Exceptions;

/// <summary>
/// Thrown when a wallet does not exist for the requested user.
/// </summary>
public sealed class WalletNotFoundException : DomainException
{
    public WalletNotFoundException(Guid userId)
        : base("WALLET_NOT_FOUND", $"Wallet for user '{userId}' was not found")
    {
    }
}

/// <summary>
/// Thrown when a wallet balance is insufficient for a charge.
/// </summary>
public sealed class InsufficientFundsException : DomainException
{
    public long AvailableCents { get; }
    public long RequiredCents { get; }

    public InsufficientFundsException(long availableCents, long requiredCents, bool useBonus = false)
        : base(
            "INSUFFICIENT_FUNDS",
            $"Insufficient {(useBonus ? "bonus " : string.Empty)}funds: {availableCents} cents available, {requiredCents} required")
    {
        AvailableCents = availableCents;
        RequiredCents = requiredCents;
    }
}

/// <summary>
/// Thrown when a tariff is not found.
/// </summary>
public sealed class TariffNotFoundException : DomainException
{
    public TariffNotFoundException(Guid tariffId)
        : base("TARIFF_NOT_FOUND", $"Tariff '{tariffId}' was not found")
    {
    }
}

/// <summary>
/// Thrown when a promocode is not found.
/// </summary>
public sealed class PromocodeNotFoundException : DomainException
{
    public PromocodeNotFoundException(string code)
        : base("PROMOCODE_NOT_FOUND", $"Promocode '{code}' was not found")
    {
    }
}

/// <summary>
/// Thrown when a promocode exists but cannot be redeemed (expired, exhausted, inactive, per-user cap).
/// </summary>
public sealed class PromocodeNotApplicableException : DomainException
{
    public PromocodeNotApplicableException(string code)
        : base("PROMOCODE_NOT_APPLICABLE", $"Promocode '{code}' cannot be used at this time")
    {
    }
}
