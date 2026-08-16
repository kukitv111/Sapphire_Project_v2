using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Enums;
using Sapphire.Auth.Domain.Repositories;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

public sealed class ActivityHistoryRepository : IActivityHistoryRepository
{
    private readonly AuthDbContext _context;

    public ActivityHistoryRepository(AuthDbContext context) => _context = context;

    public async Task<ActivityHistory?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.ActivityHistory.FindAsync([id], ct);

    public async Task<IReadOnlyList<ActivityHistory>> GetByUserIdAsync(Guid userId, int? limit = null, CancellationToken ct = default)
    {
        var query = _context.ActivityHistory
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync(ct);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityHistory>> GetByUserIdAndTypeAsync(Guid userId, ActivityType activityType, int? limit = null, CancellationToken ct = default)
    {
        var query = _context.ActivityHistory
            .Where(a => a.UserId == userId && a.ActivityType == activityType)
            .OrderByDescending(a => a.OccurredAt);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync(ct);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityHistory>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.ActivityHistory
            .Where(a => a.OccurredAt >= from && a.OccurredAt <= to)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ActivityHistory>> GetRecentAsync(int limit = 100, CancellationToken ct = default) =>
        await _context.ActivityHistory
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task AddAsync(ActivityHistory activityHistory, CancellationToken ct = default) =>
        await _context.ActivityHistory.AddAsync(activityHistory, ct);

    public async Task<int> GetFailedLoginCountAsync(Guid userId, TimeSpan within, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - within;
        return await _context.ActivityHistory
            .CountAsync(a => a.UserId == userId && a.ActivityType == ActivityType.FailedLogin && a.OccurredAt >= cutoff, ct);
    }

    public async Task<int> CleanupOldRecordsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var oldRecords = await _context.ActivityHistory
            .Where(a => a.OccurredAt < cutoff)
            .ToListAsync(ct);

        _context.ActivityHistory.RemoveRange(oldRecords);
        return oldRecords.Count;
    }
}
