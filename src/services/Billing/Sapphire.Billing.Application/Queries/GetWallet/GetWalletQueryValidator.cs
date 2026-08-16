using FluentValidation;
using Sapphire.Billing.Application.Queries.GetWallet;

namespace Sapphire.Billing.Application.Queries.GetWallet;

/// <summary>
/// Валидатор запроса получения кошелька.
/// </summary>
public sealed class GetWalletQueryValidator : AbstractValidator<GetWalletQuery>
{
    public GetWalletQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
