namespace Sapphire.Billing.Domain.Repositories;

/// <summary>
/// Unit of Work interface for the Billing context.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
