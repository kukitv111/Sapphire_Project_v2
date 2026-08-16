namespace Sapphire.Billing.Domain.Enums;

/// <summary>
/// Discount model of a promocode.
/// </summary>
public enum PromocodeType
{
    /// <summary>Discounts a percentage of the purchase amount (1-100).</summary>
    Percent,

    /// <summary>Discounts a fixed amount in cents.</summary>
    FixedAmount
}
