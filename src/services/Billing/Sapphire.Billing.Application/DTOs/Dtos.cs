namespace Sapphire.Billing.Application.DTOs;

/// <summary>
/// Data transfer object for a wallet.
/// </summary>
public sealed record WalletDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public long MainBalanceCents { get; init; }
    public long BonusBalanceCents { get; init; }

    public WalletDto() { }

    public WalletDto(Guid id, long mainBalanceCents, long bonusBalanceCents)
    {
        Id = id;
        MainBalanceCents = mainBalanceCents;
        BonusBalanceCents = bonusBalanceCents;
    }

    public WalletDto(Guid id, Guid userId, long mainBalanceCents, long bonusBalanceCents)
        : this(id, mainBalanceCents, bonusBalanceCents)
    {
        UserId = userId;
    }
}

/// <summary>
/// Data transfer object for a tariff.
/// </summary>
public sealed record TariffDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public long PricePerMinuteCents { get; init; }
    public long PricePerHourCents { get; init; }
    public int? PackageDurationMinutes { get; init; }
    public int? PackageBonusMinutes { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
}
