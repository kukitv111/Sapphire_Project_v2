using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Repositories;

namespace Sapphire.Billing.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for the Wallet aggregate.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly BillingDbContext _context;

    public WalletRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FindAsync([walletId], cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }
}
