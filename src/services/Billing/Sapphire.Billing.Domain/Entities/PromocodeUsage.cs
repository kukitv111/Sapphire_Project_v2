using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Billing.Domain.Entities;

/// <summary>
/// Per-user redemption counter of a promocode. Enforces the per-user usage cap.
/// </summary>
public sealed class PromocodeUsage : Entity
{
    public Guid PromocodeId { get; private set; }
    public Guid UserId { get; private set; }
    public int UsedCount { get; private set; }
    public DateTime FirstUsedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private PromocodeUsage()
    {
    }

    private PromocodeUsage(Guid promocodeId, Guid userId)
    {
        PromocodeId = promocodeId;
        UserId = userId;
        UsedCount = 0;
        FirstUsedAt = DateTime.UtcNow;
    }

    public static PromocodeUsage Create(Guid promocodeId, Guid userId)
    {
        if (promocodeId == Guid.Empty)
            throw new ArgumentException("Promocode ID cannot be empty", nameof(promocodeId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        return new PromocodeUsage(promocodeId, userId);
    }

    public void Increment()
    {
        UsedCount++;
        LastUsedAt = DateTime.UtcNow;
    }
}
