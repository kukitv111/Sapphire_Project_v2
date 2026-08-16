using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Role entity representing a user role in the system.
/// Roles are used for RBAC (Role-Based Access Control).
/// </summary>
public sealed class Role : Entity
{
    private readonly List<Permission> _permissions = [];
    private readonly List<UserRole> _userRoles = [];

    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role(string name, string? description, bool isSystem) : base()
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        IsSystem = isSystem;
        IsActive = true;
    }

    private Role(Guid id, string name, string? description, bool isSystem) : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        IsSystem = isSystem;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    public static Role Create(string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        if (name.Length > 50)
            throw new ArgumentException("Role name cannot exceed 50 characters", nameof(name));

        return new Role(name, description, isSystem);
    }

    /// <summary>
    /// Creates a role with a specific ID (for seeding).
    /// </summary>
    public static Role CreateWithId(Guid id, string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        if (name.Length > 50)
            throw new ArgumentException("Role name cannot exceed 50 characters", nameof(name));

        return new Role(id, name, description, isSystem);
    }

    /// <summary>
    /// Updates the role description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a permission to the role.
    /// </summary>
    public void AddPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_permissions.Any(p => p.Id == permission.Id))
            return; // Already has this permission

        _permissions.Add(permission);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a permission from the role.
    /// </summary>
    public void RemovePermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        var existing = _permissions.FirstOrDefault(p => p.Id == permission.Id);
        if (existing != null)
        {
            _permissions.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Checks if the role has a specific permission.
    /// </summary>
    public bool HasPermission(string permissionCode)
    {
        return _permissions.Any(p => p.Code.Equals(permissionCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the role has a specific permission.
    /// </summary>
    public bool HasPermission(Guid permissionId)
    {
        return _permissions.Any(p => p.Id == permissionId);
    }

    /// <summary>
    /// Deactivates the role.
    /// System roles cannot be deactivated.
    /// </summary>
    public void Deactivate()
    {
        if (IsSystem)
            throw new InvalidOperationException("System roles cannot be deactivated");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the role.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
