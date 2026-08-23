using Sapphire.Session.Domain.Aggregates;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Domain.Repositories;

/// <summary>
/// Repository port for the Session aggregate.
/// </summary>
public interface ISessionRepository
{
    /// <summary>Finds a session by id.</summary>
    Task<Result<Session>> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns sessions ordered by start time descending, limited for admin listing.</summary>
    Task<IReadOnlyList<Session>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Adds a new session.</summary>
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
}
