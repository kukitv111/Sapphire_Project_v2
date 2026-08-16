using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Commands.CreateTariff;

/// <summary>
/// Обработчик команды создания тарифа.
/// </summary>
public sealed class CreateTariffCommandHandler : IRequestHandler<CreateTariffCommand, Result<TariffDto>>
{
    private readonly ITariffRepository _tariffRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTariffCommandHandler(ITariffRepository tariffRepository, IUnitOfWork unitOfWork)
    {
        _tariffRepository = tariffRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TariffDto>> Handle(CreateTariffCommand request, CancellationToken cancellationToken)
    {
        var tariff = Tariff.Create(
            request.Name,
            request.Type,
            request.PricePerMinuteCents,
            request.PricePerHourCents,
            request.PackageDurationMinutes,
            request.PackageBonusMinutes,
            request.IsSystem);

        await _tariffRepository.AddAsync(tariff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TariffDto
        {
            Id = tariff.Id,
            Name = tariff.Name,
            Type = tariff.Type.ToString(),
            PricePerMinuteCents = tariff.PricePerMinuteCents,
            PricePerHourCents = tariff.PricePerHourCents,
            PackageDurationMinutes = tariff.PackageDurationMinutes,
            PackageBonusMinutes = tariff.PackageBonusMinutes,
            IsActive = tariff.IsActive,
            IsSystem = tariff.IsSystem
        });
    }
}
