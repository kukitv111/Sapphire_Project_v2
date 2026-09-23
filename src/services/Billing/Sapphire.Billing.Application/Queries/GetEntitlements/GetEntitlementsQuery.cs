using MediatR;
using Sapphire.Billing.Application.Commands.AssignTariffToUser;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Application.Queries.GetEntitlements;

public sealed record GetEntitlementsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<EntitlementDto>>>;

public sealed class GetEntitlementsQueryHandler(IEntitlementRepository repository)
    : IRequestHandler<GetEntitlementsQuery, Result<IReadOnlyList<EntitlementDto>>>
{
    public async Task<Result<IReadOnlyList<EntitlementDto>>> Handle(GetEntitlementsQuery request, CancellationToken ct)
    {
        var entries = await repository.GetByUserIdAsync(request.UserId, ct);
        return Result.Success<IReadOnlyList<EntitlementDto>>(entries.Select(e =>
            new EntitlementDto(e.Id, e.UserId, e.TariffId, e.PaymentTransactionId,
                e.PurchasedCents, e.AvailableCents, e.IdempotencyKey)).ToList());
    }
}
