using AutoMapper;
using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Queries.GetTariffs;

/// <summary>
/// Обработчик запроса получения тарифов.
/// </summary>
public sealed class GetTariffsQueryHandler : IRequestHandler<GetTariffsQuery, Result<IReadOnlyList<TariffDto>>>
{
    private readonly ITariffRepository _tariffRepository;
    private readonly IMapper _mapper;

    public GetTariffsQueryHandler(ITariffRepository tariffRepository, IMapper mapper)
    {
        _tariffRepository = tariffRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TariffDto>>> Handle(GetTariffsQuery request, CancellationToken cancellationToken)
    {
        var tariffs = await _tariffRepository.GetActiveAsync(cancellationToken);
        var dtos = tariffs.Select(t => _mapper.Map<TariffDto>(t)).ToList();
        return Result.Success<IReadOnlyList<TariffDto>>(dtos);
    }
}
