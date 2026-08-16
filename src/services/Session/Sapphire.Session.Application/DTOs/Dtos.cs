namespace Sapphire.Session.Application.DTOs;

/// <summary>
/// Data transfer object for a gaming session.
/// </summary>
public sealed record SessionDto
{
    public Guid Id { get; init; }
    public Guid ComputerId { get; init; }
    public Guid UserId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;

    public SessionDto() { }

    public SessionDto(Guid id, Guid computerId, Guid userId, DateTime startTime, DateTime endTime)
    {
        Id = id;
        ComputerId = computerId;
        UserId = userId;
        StartTime = startTime;
        EndTime = endTime;
        Status = "Active";
    }
}
