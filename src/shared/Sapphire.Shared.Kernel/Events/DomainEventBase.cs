namespace Sapphire.Shared.Kernel.Events;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}
