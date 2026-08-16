using MediatR;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Queries.GetTariffs;

/// <summary>
/// Запрос для получения списка активных тарифов.
/// </summary>
public sealed record GetTariffsQuery() : IRequest<Result<IReadOnlyList<TariffDto>>>;
