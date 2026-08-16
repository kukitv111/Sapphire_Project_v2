using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Domain.Repositories;

/// <summary>
/// Repository port for the Wallet aggregate.
/// </summary>
public interface IWalletRepository
{
    /// <summary>Finds the wallet of a user, or null when none exists.</summary>
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Finds a wallet by id.</summary>
    Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new wallet.</summary>
    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
