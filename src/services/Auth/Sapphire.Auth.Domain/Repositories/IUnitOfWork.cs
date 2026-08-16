using System.Threading;
using System.Threading.Tasks;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Unit of Work interface for Auth Domain.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
