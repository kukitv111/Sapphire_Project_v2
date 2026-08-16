namespace Sapphire.Billing.Domain.Enums;

/// <summary>
/// Pricing model of a tariff.
/// </summary>
public enum TariffType
{
    /// <summary>Billed per started minute.</summary>
    PerMinute,

    /// <summary>Billed per started hour.</summary>
    PerHour,

    /// <summary>Fixed-duration bundle with optional bonus minutes.</summary>
    Package
}
