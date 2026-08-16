using System.ComponentModel.DataAnnotations;

namespace Sapphire.Billing.Api.Contracts;

public record TopUpRequest(
    [Required]
    long AmountCents,
    string? Method = null,
    string? ReferenceId = null,
    string? Description = null
);

public record CreateTariffRequest(
    [Required]
    string Name,
    [Required]
    string Type,
    long? PricePerMinuteCents = null,
    long? PricePerHourCents = null,
    int? PackageDurationMinutes = null,
    int? PackageBonusMinutes = null,
    bool IsSystem = false
);
