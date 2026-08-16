namespace Sapphire.Billing.Domain.Enums;

/// <summary>
/// Direction and purpose of a wallet ledger entry.
/// Amounts are always stored as positive values; the type determines direction.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>External funds added to the wallet (cash, card, terminal).</summary>
    TopUp,

    /// <summary>Money spent on services (sessions, goods).</summary>
    Purchase,

    /// <summary>Money returned to the wallet.</summary>
    Refund,

    /// <summary>Manual correction performed by an operator.</summary>
    Adjustment,

    /// <summary>Bonus balance credited (campaign, promo).</summary>
    BonusCredit,

    /// <summary>Bonus balance spent.</summary>
    BonusDebit
}
