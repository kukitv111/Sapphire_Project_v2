using MediatR;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Billing.Application.Commands.TopUpWallet;

/// <summary>
/// Обработчик команды пополнения кошелька.
/// </summary>
public sealed class TopUpWalletCommandHandler : IRequestHandler<TopUpWalletCommand, Result>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TopUpWalletCommandHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TopUpWalletCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet == null)
            return Result.Failure(Error.NotFound($"Wallet not found for user {request.UserId}"));

        var details = string.Join(' ', new[] { request.Method, request.ReferenceId, request.Description }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        wallet.Deposit(Money.FromCents(request.AmountCents), details);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
