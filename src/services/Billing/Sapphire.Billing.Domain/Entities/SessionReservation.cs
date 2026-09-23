using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Entities;

public enum ReservationStatus { Reserved, Settled, Released }

public sealed class SessionReservation : Entity
{
    public Guid SessionId { get; private set; }
    public Guid EntitlementId { get; private set; }
    public Guid UserId { get; private set; }
    public long ReservedCents { get; private set; }
    public long ChargedCents { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime PlannedEndAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public ReservationStatus Status { get; private set; }

    private SessionReservation() { }
    public SessionReservation(Guid sessionId, Guid entitlementId, Guid userId, long reservedCents,
        DateTime startedAt, DateTime plannedEndAt)
    {
        if (sessionId == Guid.Empty || entitlementId == Guid.Empty || userId == Guid.Empty || reservedCents <= 0)
            throw new ArgumentException("Valid reservation identifiers and amount are required");
        SessionId = sessionId;
        EntitlementId = entitlementId;
        UserId = userId;
        ReservedCents = reservedCents;
        StartedAt = startedAt;
        PlannedEndAt = plannedEndAt;
        Status = ReservationStatus.Reserved;
    }
    public void Settle(long charge, DateTime endedAt)
    {
        if (Status != ReservationStatus.Reserved || charge < 0 || charge > ReservedCents)
            throw new InvalidOperationException("Reservation cannot be settled");
        ChargedCents = charge;
        CompletedAt = endedAt;
        Status = ReservationStatus.Settled;
    }
    public void Release()
    {
        if (Status != ReservationStatus.Reserved) throw new InvalidOperationException("Reservation cannot be released");
        Status = ReservationStatus.Released;
        CompletedAt = DateTime.UtcNow;
    }
}
