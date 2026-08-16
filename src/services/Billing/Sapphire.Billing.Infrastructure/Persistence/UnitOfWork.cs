using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Shared.Kernel.Events;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Billing.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BillingDbContext _context;

    public UnitOfWork(BillingDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events from all aggregates
        var domainEvents = _context.ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Convert to OutboxMessage and add to the context
        var outboxMessages = domainEvents.Select(OutboxMessage.Create);
        await _context.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);

        // Clear events after queuing
        foreach (var entry in _context.ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
