using Sapphire.Session.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Events;

namespace Sapphire.Session.Domain.Events;

/// <summary>
/// Published when a computer is added to the club.
/// </summary>
public sealed record ComputerAddedEvent(Guid ComputerId, string Model) : DomainEventBase;

/// <summary>
/// Published when a session is started on a computer.
/// </summary>
public sealed record SessionStartedEvent(Guid SessionId, Guid ComputerId, DateTime StartedAt) : DomainEventBase;

/// <summary>
/// Published when a session on a computer ends.
/// </summary>
public sealed record SessionEndedEvent(Guid ComputerId, DateTime EndedAt) : DomainEventBase;

/// <summary>
/// Published when maintenance is scheduled for a computer.
/// </summary>
public sealed record ComputerMaintenanceScheduledEvent(Guid ComputerId) : DomainEventBase;

/// <summary>
/// Published when a session is created.
/// </summary>
public sealed record SessionCreatedEvent(Guid SessionId, Guid ComputerId, Guid UserId, SessionTimeSlot TimeSlot) : DomainEventBase;

/// <summary>
/// Published when a session is completed.
/// </summary>
public sealed record SessionCompletedEvent(Guid SessionId, DateTime CompletedAt) : DomainEventBase;
