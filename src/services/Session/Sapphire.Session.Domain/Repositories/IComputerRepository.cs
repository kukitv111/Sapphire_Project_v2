using Sapphire.Session.Domain.Aggregates;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Domain.Repositories;

/// <summary>
/// Repository port for the Computer aggregate.
/// </summary>
public interface IComputerRepository
{
    /// <summary>Finds a computer by id.</summary>
    Task<Result<Computer>> GetByIdAsync(Guid computerId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new computer.</summary>
    Task AddAsync(Computer computer, CancellationToken cancellationToken = default);
}
