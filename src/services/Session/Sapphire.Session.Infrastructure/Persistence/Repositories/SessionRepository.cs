using Microsoft.EntityFrameworkCore;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly SessionDbContext _context;

    public SessionRepository(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Session>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .FindAsync(new object[] { id }, cancellationToken);

        return session != null
            ? Result.Success(session)
            : Result.Failure<Session>(Error.Create("SESSION_NOT_FOUND", "Session not found"));
    }

    public async Task<IReadOnlyList<Session>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        return await _context.Sessions
            .OrderByDescending(s => s.TimeSlot.Start)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken)
    {
        await _context.Sessions.AddAsync(session, cancellationToken);
    }
}
