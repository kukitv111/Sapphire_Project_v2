namespace Sapphire.Shared.Messaging.Outbox;

/// <summary>
/// Repository interface for outbox operations.
/// Must be implemented in each service's infrastructure layer.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unprocessed messages ordered by occurrence time.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed with error details.
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments retry count for a message.
    /// </summary>
    Task IncrementRetryCountAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages that are ready for retry.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetForRetryAsync(int maxRetryCount, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed messages.
    /// </summary>
    Task CleanupOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
