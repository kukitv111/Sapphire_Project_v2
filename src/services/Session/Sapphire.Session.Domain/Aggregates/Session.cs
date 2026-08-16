using Sapphire.Session.Domain.Events;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Session.Domain.ValueObjects;

namespace Sapphire.Session.Domain.Aggregates;

public sealed class Session : AggregateRoot
{
    public Guid ComputerId { get; private set; }
    public Guid UserId { get; private set; }
    public SessionTimeSlot TimeSlot { get; private set; }
    public SessionStatus Status { get; private set; }

    // EF Core constructor
    private Session()
    {
        TimeSlot = null!;
    }

    public Session(Guid computerId, Guid userId, SessionTimeSlot timeSlot)
    {
        ComputerId = computerId;
        UserId = userId;
        TimeSlot = timeSlot;
        Status = SessionStatus.Active;
        AddDomainEvent(new SessionCreatedEvent(Id, computerId, userId, timeSlot));
    }

    public void Complete()
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException("Cannot complete already completed session");

        Status = SessionStatus.Completed;
        AddDomainEvent(new SessionCompletedEvent(Id, DateTime.UtcNow));
    }
}

public enum SessionStatus
{
    Active,
    Completed,
    Cancelled
}
