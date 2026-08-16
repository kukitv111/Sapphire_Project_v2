using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Enums;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for ActivityHistory entity.
/// </summary>
public interface IActivityHistoryRepository
{
    /// <summary>
    /// Gets an activity history record by ID.
    /// </summary>
    Task<ActivityHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity history for a user.
    /// </summary>
    Task<IReadOnlyList<ActivityHistory>> GetByUserIdAsync(
        Guid userId, 
        int? limit = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity history for a user by activity type.
    /// </summary>
    Task<IReadOnlyList<ActivityHistory>> GetByUserIdAndTypeAsync(
        Guid userId,
        ActivityType activityType,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity history within a date range.
    /// </summary>
    Task<IReadOnlyList<ActivityHistory>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent activity history.
    /// </summary>
    Task<IReadOnlyList<ActivityHistory>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new activity history record.
    /// </summary>
    Task AddAsync(ActivityHistory activityHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts count for a user within a time window.
    /// </summary>
    Task<int> GetFailedLoginCountAsync(
        Guid userId,
        TimeSpan within,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old activity history records.
    /// </summary>
    Task<int> CleanupOldRecordsAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);
}
