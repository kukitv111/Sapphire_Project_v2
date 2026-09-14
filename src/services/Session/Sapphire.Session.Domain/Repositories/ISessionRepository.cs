using Sapphire.Shared.Kernel.Common;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Session.Domain.Repositories;

/// <summary>
/// Repository port for the Session aggregate.
/// </summary>
public interface ISessionRepository
{
    /// <summary>Finds a session by id.</summary>
    Task<Result<SessionAggregate>> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns sessions ordered by start time descending, limited for admin listing.</summary>
    Task<IReadOnlyList<SessionAggregate>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Adds a new session.</summary>
    Task AddAsync(SessionAggregate session, CancellationToken cancellationToken = default);
}
