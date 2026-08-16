using Sapphire.Billing.Domain.Enums;

namespace Sapphire.Billing.Domain.ValueObjects;

/// <summary>
/// Pure discount calculation value object. Never touches state — given a purchase
/// amount it returns the discounted amount in cents.
/// </summary>
public sealed record Discount
{
    public PromocodeType Type { get; }
    public long ValueCents { get; }

    private Discount(PromocodeType type, long valueCents)
    {
        Type = type;
        ValueCents = valueCents;
    }

    /// <summary>
    /// Creates a discount. Percent discounts must be within 1-100;
    /// fixed discounts must be positive.
    /// </summary>
    public static Discount Percent(int percent)
    {
        if (percent is < 1 or > 100)
            throw new ArgumentException("Percent discount must be between 1 and 100", nameof(percent));

        return new Discount(PromocodeType.Percent, percent);
    }

    /// <summary>
    /// Creates a fixed-amount discount in cents.
    /// </summary>
    public static Discount Fixed(long amountCents)
    {
        if (amountCents <= 0)
            throw new ArgumentException("Fixed discount must be positive", nameof(amountCents));

        return new Discount(PromocodeType.FixedAmount, amountCents);
    }

    /// <summary>
    /// Applies the discount to a purchase amount and returns the discounted amount in cents.
    /// The result is never negative and never exceeds the original amount.
    /// </summary>
    public long ApplyTo(long amountCents)
    {
        if (amountCents <= 0)
            throw new ArgumentException("Purchase amount must be positive", nameof(amountCents));

        var discounted = Type switch
        {
            PromocodeType.Percent => amountCents - (amountCents * ValueCents / 100),
            PromocodeType.FixedAmount => amountCents - ValueCents,
            _ => amountCents
        };

        return Math.Max(0, discounted);
    }

    /// <summary>
    /// Absolute saving in cents for a given purchase amount.
    /// </summary>
    public long SavingOn(long amountCents) => Math.Max(0, amountCents - ApplyTo(amountCents));
}
