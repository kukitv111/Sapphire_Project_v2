using FluentValidation;
using Sapphire.Billing.Application.Commands.TopUpWallet;

namespace Sapphire.Billing.Application.Commands.TopUpWallet;

/// <summary>
/// Валидатор команды пополнения кошелька.
/// </summary>
public sealed class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AmountCents).GreaterThan(0);
    }
}
