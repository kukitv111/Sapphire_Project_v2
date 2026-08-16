using Sapphire.Billing.Domain.Events;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Billing.Domain.Aggregates;

public sealed class Wallet : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Money MainBalance { get; private set; }
    public Money BonusBalance { get; private set; }

    // Конструктор для EF Core
    private Wallet()
    {
        MainBalance = Money.Zero;
        BonusBalance = Money.Zero;
    }

    public Wallet(Guid userId, Money initialBalance)
    {
        UserId = userId;
        MainBalance = initialBalance;
        BonusBalance = Money.Zero;
        AddDomainEvent(new WalletCreatedEvent
        {
            WalletId = Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void Deposit(Money amount, string details)
    {
        MainBalance += amount;
        AddDomainEvent(new WalletDepositedEvent
        {
            WalletId = Id,
            UserId = UserId,
            AmountCents = amount.Cents,
            Method = details,
            BalanceAfterCents = MainBalance.Cents
        });
    }

    public Result Debit(Money amount, string details)
    {
        if (MainBalance < amount)
            return Result.Failure(Error.Create("INSUFFICIENT_FUNDS", "Недостаточно средств"));

        MainBalance -= amount;
        AddDomainEvent(new WalletDebitedEvent
        {
            WalletId = Id,
            UserId = UserId,
            AmountCents = amount.Cents,
            MainAmountCents = amount.Cents,
            BonusAmountCents = 0,
            ReferenceId = details,
            BalanceAfterCents = MainBalance.Cents
        });
        return Result.Success();
    }

    public void CreditBonus(Money amount, string details)
    {
        BonusBalance += amount;
        AddDomainEvent(new WalletBonusCreditedEvent
        {
            WalletId = Id,
            UserId = UserId,
            BonusAmountCents = amount.Cents,
            ReferenceId = details,
            BonusBalanceAfterCents = BonusBalance.Cents
        });
    }

    public Result DebitBonus(Money amount, string details)
    {
        if (BonusBalance < amount)
            return Result.Failure(Error.Create("INSUFFICIENT_BONUS", "Недостаточно бонусных средств"));

        BonusBalance -= amount;
        AddDomainEvent(new WalletBonusDebitedEvent
        {
            WalletId = Id,
            UserId = UserId,
            BonusAmountCents = amount.Cents,
            ReferenceId = details,
            BonusBalanceAfterCents = BonusBalance.Cents
        });
        return Result.Success();
    }
}
