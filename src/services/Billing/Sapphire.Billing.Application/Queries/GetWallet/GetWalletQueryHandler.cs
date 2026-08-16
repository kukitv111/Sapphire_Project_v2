using AutoMapper;
using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Queries.GetWallet;

/// <summary>
/// Обработчик запроса получения кошелька.
/// </summary>
public sealed class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, Result<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IMapper _mapper;

    public GetWalletQueryHandler(IWalletRepository walletRepository, IMapper mapper)
    {
        _walletRepository = walletRepository;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet == null)
            return Result.Failure<WalletDto>(Error.NotFound($"Wallet for user {request.UserId} not found"));

        var dto = _mapper.Map<WalletDto>(wallet);
        return Result.Success(dto);
    }
}
