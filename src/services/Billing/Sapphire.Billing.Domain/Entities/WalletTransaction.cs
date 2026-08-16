using Sapphire.Billing.Domain.Enums;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Entities;

/// <summary>
/// Immutable ledger entry of a wallet. Every balance change produces exactly one entry,
/// which allows full audit reconstruction of the wallet state.
/// </summary>
public sealed class WalletTransaction : Entity
{
    public Guid WalletId { get; private set; }
    public WalletTransactionType Type { get; private set; }

    /// <summary>Main balance movement in cents. Always positive; direction is implied by <see cref="Type"/>.</summary>
    public long AmountCents { get; private set; }

    /// <summary>Bonus balance movement in cents. Always positive.</summary>
    public long BonusAmountCents { get; private set; }

    /// <summary>Main balance after this entry was applied.</summary>
    public long BalanceAfterCents { get; private set; }

    /// <summary>Bonus balance after this entry was applied.</summary>
    public long BonusBalanceAfterCents { get; private set; }

    /// <summary>Payment method for top-ups (cash, card, terminal...).</summary>
    public string? Method { get; private set; }

    /// <summary>External correlation id (payment id, session id, order id).</summary>
    public string? ReferenceId { get; private set; }

    public string? Description { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private WalletTransaction()
    {
    }

    private WalletTransaction(
        Guid walletId,
        WalletTransactionType type,
        long amountCents,
        long bonusAmountCents,
        long balanceAfterCents,
        long bonusBalanceAfterCents,
        string? method,
        string? referenceId,
        string? description)
    {
        WalletId = walletId;
        Type = type;
        AmountCents = amountCents;
        BonusAmountCents = bonusAmountCents;
        BalanceAfterCents = balanceAfterCents;
        BonusBalanceAfterCents = bonusBalanceAfterCents;
        Method = method;
        ReferenceId = referenceId;
        Description = description;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a ledger entry. Amounts must be non-negative; a zero-amount entry is rejected
    /// to keep the ledger free of noise.
    /// </summary>
    public static WalletTransaction Create(
        Guid walletId,
        WalletTransactionType type,
        long amountCents,
        long bonusAmountCents,
        long balanceAfterCents,
        long bonusBalanceAfterCents,
        string? method = null,
        string? referenceId = null,
        string? description = null)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("Wallet ID cannot be empty", nameof(walletId));

        if (amountCents < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amountCents));

        if (bonusAmountCents < 0)
            throw new ArgumentException("Bonus amount cannot be negative", nameof(bonusAmountCents));

        if (amountCents == 0 && bonusAmountCents == 0)
            throw new ArgumentException("A ledger entry must move at least one unit", nameof(amountCents));

        if (balanceAfterCents < 0)
            throw new ArgumentException("Balance after cannot be negative", nameof(balanceAfterCents));

        if (bonusBalanceAfterCents < 0)
            throw new ArgumentException("Bonus balance after cannot be negative", nameof(bonusBalanceAfterCents));

        return new WalletTransaction(
            walletId, type, amountCents, bonusAmountCents,
            balanceAfterCents, bonusBalanceAfterCents,
            method, referenceId, description);
    }
}
