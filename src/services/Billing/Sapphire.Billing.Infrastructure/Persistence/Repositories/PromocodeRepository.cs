using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;

namespace Sapphire.Billing.Infrastructure.Persistence.Repositories;

public sealed class PromocodeRepository : IPromocodeRepository
{
    private readonly BillingDbContext _context;

    public PromocodeRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Promocode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = Promocode.Normalize(code);
        return await _context.Promocodes
            .FirstOrDefaultAsync(p => p.NormalizedCode == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Promocode promocode, CancellationToken cancellationToken = default)
    {
        await _context.Promocodes.AddAsync(promocode, cancellationToken);
    }

    public async Task UpdateAsync(Promocode promocode, CancellationToken cancellationToken = default)
    {
        _context.Promocodes.Update(promocode);
        await Task.CompletedTask;
    }
}
