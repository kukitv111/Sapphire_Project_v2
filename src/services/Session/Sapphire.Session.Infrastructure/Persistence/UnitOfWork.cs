using Microsoft.EntityFrameworkCore;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Session.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork for the Session context.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SessionDbContext _context;

    public UnitOfWork(SessionDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = _context.ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity).ToList();
        var messages = aggregates.SelectMany(entity => entity.DomainEvents)
            .Select(OutboxMessage.Create).ToList();
        _context.OutboxMessages.AddRange(messages);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Preserve events and remove only this attempt's staged messages for retry.
            foreach (var message in messages)
                _context.Entry(message).State = EntityState.Detached;
            throw;
        }
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();
    }
}
