using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for Role entity.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by name.
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active roles.
    /// </summary>
    Task<IReadOnlyList<Role>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles by IDs.
    /// </summary>
    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role.
    /// </summary>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    void Update(Role role);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    void Delete(Role role);

    /// <summary>
    /// Checks if a role name is already taken.
    /// </summary>
    Task<bool> IsNameTakenAsync(string name, CancellationToken cancellationToken = default);
}
