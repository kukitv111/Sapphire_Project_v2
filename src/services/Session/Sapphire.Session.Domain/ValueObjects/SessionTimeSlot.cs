using Sapphire.Shared.Kernel.Common;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Session.Domain.ValueObjects;

public sealed record SessionTimeSlot : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private SessionTimeSlot(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static Result<SessionTimeSlot> Create(DateTime start, DateTime end)
    {
        if (end <= start)
            return Result.Failure<SessionTimeSlot>(Error.Create("INVALID_TIME_SLOT", "End time must be after start time"));

        if (end - start > TimeSpan.FromHours(3))
            return Result.Failure<SessionTimeSlot>(Error.Create("SESSION_TOO_LONG", "Session cannot exceed 3 hours"));

        return new SessionTimeSlot(start, end);
    }
}
