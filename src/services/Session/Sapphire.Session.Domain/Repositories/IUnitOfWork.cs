namespace Sapphire.Session.Domain.Repositories;

/// <summary>
/// Unit of Work interface for the Session context.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
