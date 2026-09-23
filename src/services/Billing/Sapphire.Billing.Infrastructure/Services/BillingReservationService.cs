using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sapphire.Billing.Domain.Entities;
using Sapphire.Billing.Infrastructure.Persistence;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Infrastructure.Services;

public sealed record ReserveRequest(Guid SessionId, Guid UserId, Guid EntitlementId, DateTime StartedAt, DateTime PlannedEndAt);
public sealed record ReservationReceipt(Guid ReservationId, Guid SessionId, long ReservedCents);

public sealed class BillingReservationService(BillingDbContext db)
{
    public async Task<Result<ReservationReceipt>> ReserveAsync(ReserveRequest request, CancellationToken ct = default)
    {
        if (request.SessionId == Guid.Empty || request.UserId == Guid.Empty || request.EntitlementId == Guid.Empty
            || request.PlannedEndAt <= request.StartedAt || request.PlannedEndAt - request.StartedAt > TimeSpan.FromHours(3))
            return Result.Failure<ReservationReceipt>(Error.Validation("Invalid reservation request"));
        if (db.Database.IsRelational())
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            var previous = await db.SessionReservations.FirstOrDefaultAsync(r => r.SessionId == request.SessionId, ct);
            if (previous is not null)
            {
                await RollbackAsync(ct);
                return previous.UserId == request.UserId && previous.EntitlementId == request.EntitlementId
                    && previous.Status == ReservationStatus.Reserved
                    ? Result.Success(new ReservationReceipt(previous.Id, previous.SessionId, previous.ReservedCents))
                    : Result.Failure<ReservationReceipt>(Error.Conflict("Session reservation already exists"));
            }
            var entitlement = await db.Entitlements.FirstOrDefaultAsync(e => e.Id == request.EntitlementId, ct);
            if (entitlement is null || entitlement.UserId != request.UserId)
            {
                await RollbackAsync(ct);
                return Result.Failure<ReservationReceipt>(Error.NotFound("Entitlement not found"));
            }
            var amount = entitlement.CostFor(request.PlannedEndAt - request.StartedAt);
            if (!entitlement.TryReserve(amount))
            {
                await RollbackAsync(ct);
                return Result.Failure<ReservationReceipt>(Error.Conflict("Insufficient prepaid tariff balance"));
            }
            var reservation = new SessionReservation(request.SessionId, entitlement.Id,
                request.UserId, amount, request.StartedAt, request.PlannedEndAt);
            db.SessionReservations.Add(reservation);
            await db.SaveChangesAsync(ct);
            if (db.Database.CurrentTransaction is { } transaction) await transaction.CommitAsync(ct);
            return Result.Success(new ReservationReceipt(reservation.Id, request.SessionId, amount));
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(ct);
            return Result.Failure<ReservationReceipt>(Error.Conflict("Balance changed concurrently; retry with a new session"));
        }
        catch (Exception ex) when (IsWriteConflict(ex))
        {
            await RollbackAsync(ct);
            return Result.Failure<ReservationReceipt>(Error.Conflict("Reservation changed concurrently; retry"));
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result> ReleaseAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (db.Database.IsRelational()) await db.Database.BeginTransactionAsync(ct);
        try
        {
            await ReleaseInCurrentTransactionAsync(sessionId, ct);
            await db.SaveChangesAsync(ct);
            if (db.Database.CurrentTransaction is { } transaction) await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch { await RollbackAsync(ct); throw; }
    }

    public async Task ReleaseInCurrentTransactionAsync(Guid sessionId, CancellationToken ct)
    {
        var reservation = await db.SessionReservations.SingleAsync(r => r.SessionId == sessionId, ct);
        if (reservation.Status == ReservationStatus.Released) return;
        if (reservation.Status == ReservationStatus.Settled)
            throw new InvalidOperationException("Settled reservation cannot be refunded");
        var entitlement = await db.Entitlements.SingleAsync(e => e.Id == reservation.EntitlementId, ct);
        entitlement.Release(reservation.ReservedCents);
        reservation.Release();
    }

    // The inbox caller commits settlement and its receipt in one Billing database transaction.
    public async Task SettleInCurrentTransactionAsync(Guid sessionId, DateTime endedAt, CancellationToken ct)
    {
        var reservation = await db.SessionReservations.SingleAsync(r => r.SessionId == sessionId, ct);
        if (reservation.Status == ReservationStatus.Settled) return;
        if (reservation.Status != ReservationStatus.Reserved)
            throw new InvalidOperationException("Reservation was released before completion");
        var entitlement = await db.Entitlements.SingleAsync(e => e.Id == reservation.EntitlementId, ct);
        var effectiveEnd = endedAt < reservation.StartedAt ? reservation.StartedAt : endedAt;
        if (effectiveEnd > reservation.PlannedEndAt) effectiveEnd = reservation.PlannedEndAt;
        var charge = Math.Min(reservation.ReservedCents, entitlement.CostFor(effectiveEnd - reservation.StartedAt));
        entitlement.Settle(reservation.ReservedCents, charge);
        reservation.Settle(charge, endedAt);
    }

    private async Task RollbackAsync(CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is { } transaction) await transaction.RollbackAsync(ct);
    }

    private static bool IsWriteConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres && postgres.SqlState is "40001" or "23505")
                return true;
        return false;
    }
}
