using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;

namespace Sapphire.Billing.Infrastructure.Persistence.Repositories;

public sealed class TariffRepository : ITariffRepository
{
    private readonly BillingDbContext _context;

    public TariffRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Tariff?> GetByIdAsync(Guid tariffId, CancellationToken cancellationToken = default)
    {
        return await _context.Tariffs
            .FindAsync([tariffId], cancellationToken);
    }

    public async Task<IReadOnlyList<Tariff>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tariffs
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tariff>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tariffs
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tariff tariff, CancellationToken cancellationToken = default)
    {
        await _context.Tariffs.AddAsync(tariff, cancellationToken);
    }
}
