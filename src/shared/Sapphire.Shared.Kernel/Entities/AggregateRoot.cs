using Sapphire.Shared.Kernel.Events;

namespace Sapphire.Shared.Kernel.Entities;

/// <summary>
/// Base class for aggregate roots in DDD.
/// Aggregate roots are the only entry point to modify aggregates.
/// They maintain consistency boundaries and emit domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() : base() { }

    protected new void AddDomainEvent(IDomainEvent domainEvent)
    {
        base.AddDomainEvent(domainEvent);
    }
}
