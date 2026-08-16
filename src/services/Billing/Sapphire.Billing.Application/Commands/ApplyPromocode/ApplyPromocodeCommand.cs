using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Billing.Application.Commands.ApplyPromocode;

public sealed record ApplyPromocodeCommand : IRequest<Result<WalletDto>>
{
    public Guid WalletId { get; set; }
    public string PromocodeCode { get; set; } = string.Empty;
}

public sealed class ApplyPromocodeCommandHandler : IRequestHandler<ApplyPromocodeCommand, Result<WalletDto>>
{
    private readonly IPromocodeRepository _promocodeRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyPromocodeCommandHandler(
        IPromocodeRepository promocodeRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _promocodeRepository = promocodeRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WalletDto>> Handle(ApplyPromocodeCommand request, CancellationToken cancellationToken)
    {
        // Получение кошелька
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet is null)
            return Result.Failure<WalletDto>(Error.NotFound($"Wallet '{request.WalletId}' not found"));

        // Получение промокода
        var promocode = await _promocodeRepository.GetByCodeAsync(request.PromocodeCode, cancellationToken);
        if (promocode is null)
            return Result.Failure<WalletDto>(Error.Create("PROMOCODE_NOT_FOUND", "Промокод не найден"));

        // Проверка валидности промокода
        if (!promocode.CanBeUsed(wallet.UserId, DateTime.UtcNow))
            return Result.Failure<WalletDto>(Error.Create("PROMOCODE_INVALID", "Промокод недействителен или исчерпан"));

        // Применение промокода — начисляем бонус в размере номинала скидки
        promocode.RecordUsage(wallet.UserId, promocode.ValueCents);
        wallet.CreditBonus(Money.FromCents(promocode.ValueCents), $"Промокод {promocode.Code}");

        // Сохранение изменений
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        return Result.Success(new WalletDto(wallet.Id, wallet.MainBalance.Cents, wallet.BonusBalance.Cents));
    }
}
