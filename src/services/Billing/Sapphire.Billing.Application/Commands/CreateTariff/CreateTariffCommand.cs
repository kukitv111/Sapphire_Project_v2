using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Commands.CreateTariff;

/// <summary>
/// Команда создания тарифа.
/// </summary>
public sealed record CreateTariffCommand(
    string Name,
    TariffType Type,
    long? PricePerMinuteCents = null,
    long? PricePerHourCents = null,
    int? PackageDurationMinutes = null,
    int? PackageBonusMinutes = null,
    bool IsSystem = false) : IRequest<Result<TariffDto>>;
