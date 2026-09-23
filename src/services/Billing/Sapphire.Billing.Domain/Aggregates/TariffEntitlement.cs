using Sapphire.Billing.Domain.Enums;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Aggregates;

/// <summary>Prepaid tariff credit. A purchase is immutable; reservations move its available credit.</summary>
public sealed class TariffEntitlement : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid TariffId { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public TariffType Type { get; private set; }
    public long PricePerMinuteCents { get; private set; }
    public long PricePerHourCents { get; private set; }
    public long PackagePriceCents { get; private set; }
    public int PackageMinutes { get; private set; }
    public long PurchasedCents { get; private set; }
    public long AvailableCents { get; private set; }
    public long ReservedCents { get; private set; }

    private TariffEntitlement() { }
    public TariffEntitlement(Guid userId, Tariff tariff, Guid paymentTransactionId, string idempotencyKey)
    {
        if (userId == Guid.Empty || tariff.Id == Guid.Empty || !tariff.IsActive)
            throw new ArgumentException("Active tariff and user are required");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new ArgumentException("Idempotency key must be 1-128 characters", nameof(idempotencyKey));
        UserId = userId;
        TariffId = tariff.Id;
        PaymentTransactionId = paymentTransactionId;
        IdempotencyKey = idempotencyKey;
        Type = tariff.Type;
        PricePerMinuteCents = tariff.PricePerMinuteCents;
        PricePerHourCents = tariff.PricePerHourCents;
        PackagePriceCents = tariff.PackagePriceCents;
        PackageMinutes = (tariff.PackageDurationMinutes ?? 0) + (tariff.PackageBonusMinutes ?? 0);
        PurchasedCents = Type switch
        {
            TariffType.PerMinute => checked(PricePerMinuteCents * 60),
            TariffType.PerHour => PricePerHourCents,
            TariffType.Package => PackagePriceCents,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (PurchasedCents <= 0) throw new ArgumentException("Tariff has no purchase price");
        AvailableCents = PurchasedCents;
    }

    public long CostFor(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return 0;
        var minutes = checked((long)Math.Ceiling(duration.TotalMinutes));
        return Type switch
        {
            TariffType.PerMinute => checked(minutes * PricePerMinuteCents),
            TariffType.PerHour => checked((long)Math.Ceiling(duration.TotalHours) * PricePerHourCents),
            TariffType.Package => checked((long)Math.Ceiling((decimal)minutes * PackagePriceCents / PackageMinutes)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public bool TryReserve(long cents)
    {
        if (cents <= 0 || cents > AvailableCents) return false;
        AvailableCents -= cents;
        ReservedCents = checked(ReservedCents + cents);
        return true;
    }

    public void Settle(long reserved, long charge)
    {
        if (reserved <= 0 || charge < 0 || charge > reserved || reserved > ReservedCents)
            throw new InvalidOperationException("Invalid settlement");
        ReservedCents -= reserved;
        AvailableCents = checked(AvailableCents + reserved - charge);
    }

    public void Release(long reserved) => Settle(reserved, 0);
}
