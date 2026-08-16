using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Domain.Repositories;

/// <summary>
/// Repository port for the Tariff aggregate.
/// </summary>
public interface ITariffRepository
{
    /// <summary>Finds a tariff by id.</summary>
    Task<Tariff?> GetByIdAsync(Guid tariffId, CancellationToken cancellationToken = default);

    /// <summary>Returns all active tariffs, ordered by name.</summary>
    Task<IReadOnlyList<Tariff>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all tariffs (including inactive).</summary>
    Task<IReadOnlyList<Tariff>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new tariff.</summary>
    Task AddAsync(Tariff tariff, CancellationToken cancellationToken = default);
}
