namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Represents the association between a User and a Role.
/// This is a value object stored as part of the User aggregate.
/// </summary>
public sealed record UserRole
{
    public Guid RoleId { get; private init; }
    public Guid AssignedBy { get; private init; }
    public DateTime AssignedAt { get; private init; }

    private UserRole()
    {
        // For EF Core
    }

    private UserRole(Guid roleId, Guid assignedBy)
    {
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new UserRole association.
    /// </summary>
    public static UserRole Create(Guid roleId, Guid assignedBy)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID cannot be empty", nameof(roleId));

        if (assignedBy == Guid.Empty)
            throw new ArgumentException("Assigned by cannot be empty", nameof(assignedBy));

        return new UserRole(roleId, assignedBy);
    }

    /// <summary>
    /// Restores a UserRole from persisted state (used by EF Core materialization).
    /// </summary>
    public static UserRole Restore(Guid roleId, Guid assignedBy, DateTime assignedAt)
    {
        return new UserRole(roleId, assignedBy) { AssignedAt = assignedAt };
    }
}
