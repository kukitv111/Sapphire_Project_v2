using Sapphire.Billing.Domain.Entities;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Domain.Events;
using Sapphire.Billing.Domain.Exceptions;
using Sapphire.Billing.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Aggregates;

/// <summary>
/// Promocode aggregate root — a time-limited discount code with usage caps.
/// Tracks total usage and per-user usage to enforce limits.
/// </summary>
public sealed class Promocode : AggregateRoot
{
    private readonly List<PromocodeUsage> _usages = [];

    public string Code { get; private set; }

    /// <summary>Normalized code used for lookup (uppercase, trimmed).</summary>
    public string NormalizedCode { get; private set; }

    public PromocodeType Type { get; private set; }

    /// <summary>Discount value: percent (1-100) or fixed amount in cents.</summary>
    public long ValueCents { get; private set; }

    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }

    /// <summary>Maximum total redemptions. Null = unlimited.</summary>
    public int? MaxTotalUses { get; private set; }

    /// <summary>Maximum redemptions per user. Null = unlimited.</summary>
    public int? MaxUsesPerUser { get; private set; }

    public bool IsActive { get; private set; }
    public int UsedCount { get; private set; }

    public IReadOnlyCollection<PromocodeUsage> Usages => _usages.AsReadOnly();

    /// <summary>Discount model derived from the promocode configuration.</summary>
    public Discount Discount => Type == PromocodeType.Percent
        ? Discount.Percent((int)ValueCents)
        : Discount.Fixed(ValueCents);

    private Promocode()
    {
        Code = string.Empty;
        NormalizedCode = string.Empty;
    }

    private Promocode(
        string code,
        PromocodeType type,
        long valueCents,
        DateTime validFrom,
        DateTime validTo,
        int? maxTotalUses,
        int? maxUsesPerUser)
    {
        Code = code;
        NormalizedCode = Normalize(code);
        Type = type;
        ValueCents = valueCents;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MaxTotalUses = maxTotalUses;
        MaxUsesPerUser = maxUsesPerUser;
        IsActive = true;
        UsedCount = 0;
    }

    /// <summary>
    /// Creates a promocode. Code is trimmed and upper-cased for lookup.
    /// </summary>
    public static Promocode Create(
        string code,
        PromocodeType type,
        long valueCents,
        DateTime validFrom,
        DateTime validTo,
        int? maxTotalUses = null,
        int? maxUsesPerUser = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Promocode cannot be empty", nameof(code));

        if (code.Trim().Length > 50)
            throw new ArgumentException("Promocode cannot exceed 50 characters", nameof(code));

        if (type == PromocodeType.Percent && valueCents is < 1 or > 100)
            throw new ArgumentException("Percent promocode value must be between 1 and 100", nameof(valueCents));

        if (type == PromocodeType.FixedAmount && valueCents <= 0)
            throw new ArgumentException("Fixed promocode value must be positive", nameof(valueCents));

        if (validFrom >= validTo)
            throw new ArgumentException("ValidFrom must be earlier than ValidTo", nameof(validFrom));

        if (maxTotalUses is <= 0)
            throw new ArgumentException("MaxTotalUses must be positive", nameof(maxTotalUses));

        if (maxUsesPerUser is <= 0)
            throw new ArgumentException("MaxUsesPerUser must be positive", nameof(maxUsesPerUser));

        var promocode = new Promocode(code.Trim(), type, valueCents, validFrom, validTo, maxTotalUses, maxUsesPerUser);

        promocode.AddDomainEvent(new PromocodeCreatedEvent
        {
            PromocodeId = promocode.Id,
            Code = promocode.Code,
            Type = promocode.Type
        });

        return promocode;
    }

    /// <summary>
    /// Checks whether the promocode can be redeemed by the user at the given moment.
    /// </summary>
    public bool CanBeUsed(Guid userId, DateTime utcNow)
    {
        if (!IsActive)
            return false;

        if (utcNow < ValidFrom || utcNow > ValidTo)
            return false;

        if (MaxTotalUses.HasValue && UsedCount >= MaxTotalUses.Value)
            return false;

        if (MaxUsesPerUser.HasValue)
        {
            var userUses = _usages
                .Where(u => u.UserId == userId)
                .Sum(u => u.UsedCount);

            if (userUses >= MaxUsesPerUser.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Records a redemption. Throws when the promocode is not redeemable —
    /// callers should check <see cref="CanBeUsed"/> first for friendly errors.
    /// </summary>
    public void RecordUsage(Guid userId, long purchaseAmountCents)
    {
        var utcNow = DateTime.UtcNow;

        if (!CanBeUsed(userId, utcNow))
            throw new PromocodeNotApplicableException(Code);

        var usage = _usages.FirstOrDefault(u => u.UserId == userId);
        if (usage == null)
        {
            usage = PromocodeUsage.Create(Id, userId);
            _usages.Add(usage);
        }

        usage.Increment();

        UsedCount++;
        UpdatedAt = utcNow;

        AddDomainEvent(new PromocodeAppliedEvent
        {
            PromocodeId = Id,
            Code = Code,
            UserId = userId,
            PurchaseAmountCents = purchaseAmountCents,
            DiscountedAmountCents = Discount.ApplyTo(purchaseAmountCents)
        });
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Normalizes a code for storage and lookup.
    /// </summary>
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
