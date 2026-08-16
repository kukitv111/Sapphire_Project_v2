using MediatR;

namespace Sapphire.Shared.Kernel.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain.
/// They are dispatched after the transaction completes.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
