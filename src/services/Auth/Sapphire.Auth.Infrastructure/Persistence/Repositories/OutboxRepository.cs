using Microsoft.EntityFrameworkCore;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AuthDbContext _context;

    public OutboxRepository(AuthDbContext context) => _context = context;

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _context.OutboxMessages.AddAsync(message, ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize = 100, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync([messageId], ct);
        if (message != null)
        {
            message.ProcessedOn = DateTime.UtcNow;
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync([messageId], ct);
        if (message != null)
        {
            message.Error = error;
        }
    }

    public async Task IncrementRetryCountAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync([messageId], ct);
        if (message != null)
        {
            message.RetryCount++;
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetForRetryAsync(int maxRetryCount, int batchSize, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.Error != null && m.RetryCount < maxRetryCount)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task CleanupOldMessagesAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var oldMessages = await _context.OutboxMessages
            .Where(m => m.ProcessedOn != null && m.ProcessedOn < cutoff)
            .ToListAsync(ct);

        _context.OutboxMessages.RemoveRange(oldMessages);
    }
}
