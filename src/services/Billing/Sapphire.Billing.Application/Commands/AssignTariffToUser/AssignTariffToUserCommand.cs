using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Enums;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Billing.Application.Commands.AssignTariffToUser;

public sealed record AssignTariffToUserCommand : IRequest<Result<WalletDto>>
{
    public Guid UserId { get; set; }
    public Guid TariffId { get; set; }
}

public sealed class AssignTariffToUserCommandHandler : IRequestHandler<AssignTariffToUserCommand, Result<WalletDto>>
{
    private readonly ITariffRepository _tariffRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTariffToUserCommandHandler(
        ITariffRepository tariffRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _tariffRepository = tariffRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WalletDto>> Handle(AssignTariffToUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Get tariff
        var tariff = await _tariffRepository.GetByIdAsync(request.TariffId, cancellationToken);
        if (tariff is null)
            return Result.Failure<WalletDto>(Error.Create("TARIFF_NOT_FOUND", "Тариф не найден"));

        // 2. Get or create user's wallet
        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet is null)
        {
            wallet = new Domain.Aggregates.Wallet(request.UserId, Money.Zero);
            await _walletRepository.AddAsync(wallet, cancellationToken);
        }

        // 3. Apply tariff (example: add bonus based on the tariff price)
        var bonusCents = tariff.Type switch
        {
            TariffType.PerMinute => tariff.PricePerMinuteCents,
            TariffType.PerHour => tariff.PricePerHourCents,
            _ => 0
        };
        wallet.CreditBonus(Money.FromCents(bonusCents), $"Тариф {tariff.Name}");

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Return updated wallet
        return Result.Success(new WalletDto(wallet.Id, wallet.MainBalance.Cents, wallet.BonusBalance.Cents));
    }
}
