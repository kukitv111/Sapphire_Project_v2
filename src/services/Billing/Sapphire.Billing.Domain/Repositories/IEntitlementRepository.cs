using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Entities;

namespace Sapphire.Billing.Domain.Repositories;

public interface IEntitlementRepository
{
    Task<TariffEntitlement?> GetByIdempotencyKeyAsync(Guid userId, string key, CancellationToken ct);
    Task<IReadOnlyList<TariffEntitlement>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(TariffEntitlement entitlement, WalletTransaction payment, CancellationToken ct);
}
