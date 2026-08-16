using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Permission entity representing a granular permission in the system.
/// Permissions are the atomic units of access control.
/// </summary>
public sealed class Permission : Entity
{
    private readonly List<RolePermission> _rolePermissions = [];

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Category { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission(string code, string name, string category, string? description) : base()
    {
        Code = code.ToUpperInvariant();
        Name = name;
        Category = category;
        Description = description;
        IsActive = true;
    }

    private Permission(Guid id, string code, string name, string category, string? description) : base(id)
    {
        Code = code.ToUpperInvariant();
        Name = name;
        Category = category;
        Description = description;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    public static Permission Create(string code, string name, string category, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Permission category cannot be empty", nameof(category));

        if (code.Length > 100)
            throw new ArgumentException("Permission code cannot exceed 100 characters", nameof(code));

        if (name.Length > 100)
            throw new ArgumentException("Permission name cannot exceed 100 characters", nameof(name));

        return new Permission(code, name, category, description);
    }

    /// <summary>
    /// Creates a permission with a specific ID (for seeding).
    /// </summary>
    public static Permission CreateWithId(Guid id, string code, string name, string category, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Permission category cannot be empty", nameof(category));

        return new Permission(id, code, name, category, description);
    }

    /// <summary>
    /// Updates the permission details.
    /// </summary>
    public void Update(string name, string? description, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Permission category cannot be empty", nameof(category));

        Name = name;
        Description = description;
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the permission.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the permission.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
