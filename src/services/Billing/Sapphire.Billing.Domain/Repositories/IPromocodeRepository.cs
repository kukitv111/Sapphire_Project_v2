using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Domain.Repositories;

/// <summary>
/// Repository port for the Promocode aggregate.
/// </summary>
public interface IPromocodeRepository
{
    /// <summary>Finds a promocode by its normalized code.</summary>
    Task<Promocode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Adds a new promocode.</summary>
    Task AddAsync(Promocode promocode, CancellationToken cancellationToken = default);
}
