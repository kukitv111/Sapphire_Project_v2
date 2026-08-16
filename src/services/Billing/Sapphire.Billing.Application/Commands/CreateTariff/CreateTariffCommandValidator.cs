using FluentValidation;
using Sapphire.Billing.Application.Commands.CreateTariff;
using Sapphire.Billing.Domain.Enums;

namespace Sapphire.Billing.Application.Commands.CreateTariff;

/// <summary>
/// Валидатор команды создания тарифа.
/// </summary>
public sealed class CreateTariffCommandValidator : AbstractValidator<CreateTariffCommand>
{
    public CreateTariffCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        When(x => x.Type == TariffType.PerMinute,
            () => RuleFor(x => x.PricePerMinuteCents)
                .Must(p => p.HasValue && p.Value > 0).WithMessage("Per-minute price is required and must be positive"));

        When(x => x.Type == TariffType.PerHour,
            () => RuleFor(x => x.PricePerHourCents)
                .Must(p => p.HasValue && p.Value > 0).WithMessage("Per-hour price is required and must be positive"));

        When(x => x.Type == TariffType.Package,
            () =>
            {
                RuleFor(x => x.PackageDurationMinutes)
                    .Must(d => d.HasValue && d.Value > 0).WithMessage("Package duration must be positive");

                RuleFor(x => x.PackageBonusMinutes)
                    .Must(b => b.HasValue && b.Value >= 0).WithMessage("Bonus minutes must be non-negative");
            });
    }
}
