using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Entities;
using Sapphire.Billing.Domain.Repositories;

namespace Sapphire.Billing.Infrastructure.Persistence.Repositories;

public sealed class EntitlementRepository(BillingDbContext db) : IEntitlementRepository
{
    public Task<TariffEntitlement?> GetByIdempotencyKeyAsync(Guid userId, string key, CancellationToken ct)
        => db.Entitlements.FirstOrDefaultAsync(e => e.UserId == userId && e.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<TariffEntitlement>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => await db.Entitlements.AsNoTracking().Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(TariffEntitlement entitlement, WalletTransaction payment, CancellationToken ct)
    {
        await db.Entitlements.AddAsync(entitlement, ct);
        await db.WalletTransactions.AddAsync(payment, ct);
    }
}
