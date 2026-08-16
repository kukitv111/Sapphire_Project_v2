namespace Sapphire.Shared.Kernel.ValueObjects;

/// <summary>
/// Money value object for financial operations.
/// Uses integer cents to avoid floating-point precision issues.
/// All amounts are stored as cents (hundredths of the base currency).
/// </summary>
public sealed record Money : ValueObject
{
    /// <summary>
    /// Amount in cents. Always positive or zero.
    /// </summary>
    public long Cents { get; }

    private Money(long cents)
    {
        Cents = cents;
    }

    /// <summary>
    /// Creates a Money instance from cents.
    /// </summary>
    public static Money FromCents(long cents)
    {
        if (cents < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(cents));

        return new Money(cents);
    }

    /// <summary>
    /// Creates a Money instance from a decimal amount.
    /// Example: FromDecimal(10.50m) creates Money with 1050 cents.
    /// </summary>
    public static Money FromDecimal(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));

        var cents = (long)Math.Round(amount * 100);
        return new Money(cents);
    }

    /// <summary>
    /// Creates a zero Money amount.
    /// </summary>
    public static Money Zero => new(0);

    /// <summary>
    /// Converts to decimal representation.
    /// </summary>
    public decimal ToDecimal() => Cents / 100m;

    /// <summary>
    /// Adds two Money amounts.
    /// </summary>
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Money(Cents + other.Cents);
    }

    /// <summary>
    /// Subtracts another Money amount from this one.
    /// Throws if result would be negative.
    /// </summary>
    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        
        if (other.Cents > Cents)
            throw new InvalidOperationException("Cannot subtract: insufficient funds");

        return new Money(Cents - other.Cents);
    }

    /// <summary>
    /// Multiplies the Money amount by a factor.
    /// </summary>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative", nameof(factor));

        var newCents = (long)Math.Round(Cents * factor);
        return new Money(newCents);
    }

    /// <summary>
    /// Checks if this Money amount is greater than another.
    /// </summary>
    public bool IsGreaterThan(Money other) => Cents > other.Cents;

    /// <summary>
    /// Checks if this Money amount is less than another.
    /// </summary>
    public bool IsLessThan(Money other) => Cents < other.Cents;

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static bool operator >(Money a, Money b) => a.IsGreaterThan(b);
    public static bool operator <(Money a, Money b) => a.IsLessThan(b);
    public static bool operator >=(Money a, Money b) => a.Cents >= b.Cents;
    public static bool operator <=(Money a, Money b) => a.Cents <= b.Cents;

    public override string ToString() => $"{Cents / 100m:F2}";
}
