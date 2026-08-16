using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Domain.Events;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Aggregates;

/// <summary>
/// Tariff aggregate root — a pricing model for game time.
/// Per-minute and per-hour tariffs carry an hourly/daily rate; package tariffs
/// bundle a fixed duration with optional bonus minutes.
/// All prices are integer cents.
/// </summary>
public sealed class Tariff : AggregateRoot
{
    public string Name { get; private set; }
    public TariffType Type { get; private set; }

    /// <summary>Price per minute in cents (PerMinute tariffs).</summary>
    public long PricePerMinuteCents { get; private set; }

    /// <summary>Price per hour in cents (PerHour tariffs).</summary>
    public long PricePerHourCents { get; private set; }

    /// <summary>Billable duration in minutes (Package tariffs).</summary>
    public int? PackageDurationMinutes { get; private set; }

    /// <summary>Bonus minutes granted with the package.</summary>
    public int? PackageBonusMinutes { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    private Tariff()
    {
        Name = string.Empty;
    }

    private Tariff(string name, TariffType type, long pricePerMinuteCents, long pricePerHourCents,
        int? packageDurationMinutes, int? packageBonusMinutes, bool isSystem)
    {
        Name = name;
        Type = type;
        PricePerMinuteCents = pricePerMinuteCents;
        PricePerHourCents = pricePerHourCents;
        PackageDurationMinutes = packageDurationMinutes;
        PackageBonusMinutes = packageBonusMinutes;
        IsActive = true;
        IsSystem = isSystem;
    }

    /// <summary>
    /// Creates a tariff. Price fields are validated against the tariff type.
    /// </summary>
    public static Tariff Create(
        string name,
        TariffType type,
        long? pricePerMinuteCents = null,
        long? pricePerHourCents = null,
        int? packageDurationMinutes = null,
        int? packageBonusMinutes = null,
        bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tariff name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Tariff name cannot exceed 100 characters", nameof(name));

        var tariff = type switch
        {
            TariffType.PerMinute => new Tariff(name, type, RequirePositive(pricePerMinuteCents, nameof(pricePerMinuteCents)), 0, null, null, isSystem),
            TariffType.PerHour => new Tariff(name, type, 0, RequirePositive(pricePerHourCents, nameof(pricePerHourCents)), null, null, isSystem),
            TariffType.Package => new Tariff(
                name, type, 0, 0,
                (int)RequirePositive(packageDurationMinutes, nameof(packageDurationMinutes)),
                packageBonusMinutes is > 0 ? packageBonusMinutes : null,
                isSystem),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown tariff type")
        };

        tariff.AddDomainEvent(new TariffCreatedEvent
        {
            TariffId = tariff.Id,
            Name = tariff.Name,
            Type = tariff.Type
        });

        return tariff;
    }

    /// <summary>
    /// Updates prices. Package duration/bonus cannot be changed after creation.
    /// </summary>
    public void UpdatePrices(long? pricePerMinuteCents = null, long? pricePerHourCents = null)
    {
        if (Type == TariffType.PerMinute)
        {
            PricePerMinuteCents = pricePerMinuteCents is > 0
                ? pricePerMinuteCents.Value
                : throw new ArgumentException("Per-minute tariff requires a positive minute price", nameof(pricePerMinuteCents));
        }
        else if (Type == TariffType.PerHour)
        {
            PricePerHourCents = pricePerHourCents is > 0
                ? pricePerHourCents.Value
                : throw new ArgumentException("Per-hour tariff requires a positive hour price", nameof(pricePerHourCents));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsSystem)
            throw new InvalidOperationException("System tariffs cannot be deactivated");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static long RequirePositive(long? value, string paramName)
    {
        if (value is null or <= 0)
            throw new ArgumentException("Value must be positive", paramName);

        return value.Value;
    }
}
