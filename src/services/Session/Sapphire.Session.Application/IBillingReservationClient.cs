using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Application;

public interface IBillingReservationClient
{
    Task<Result> ReserveAsync(Guid sessionId, Guid userId, Guid entitlementId,
        DateTime startedAt, DateTime plannedEndAt, CancellationToken ct);
    Task<Result> ReleaseAsync(Guid sessionId, CancellationToken ct);
}
