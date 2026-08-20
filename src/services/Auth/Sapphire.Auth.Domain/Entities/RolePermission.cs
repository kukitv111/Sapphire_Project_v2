namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Represents the association between a Role and a Permission.
/// This is a value object stored as part of the Role entity.
/// </summary>
public sealed record RolePermission
{
    public Guid PermissionId { get; init; }
    public DateTime AssignedAt { get; init; }

    private RolePermission() 
    { 
        // For EF Core
    }

    private RolePermission(Guid permissionId)
    {
        PermissionId = permissionId;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new RolePermission association.
    /// </summary>
    public static RolePermission Create(Guid permissionId)
    {
        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission ID cannot be empty", nameof(permissionId));

        return new RolePermission(permissionId);
    }

    /// <summary>
    /// Restores a RolePermission from persisted state (used by EF Core materialization).
    /// </summary>
    public static RolePermission Restore(Guid permissionId, DateTime assignedAt)
    {
        return new RolePermission(permissionId) { AssignedAt = assignedAt };
    }
}
