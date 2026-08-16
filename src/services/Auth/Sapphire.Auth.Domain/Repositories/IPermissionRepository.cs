using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for Permission entity.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Gets a permission by ID.
    /// </summary>
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a permission by code.
    /// </summary>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions.
    /// </summary>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active permissions.
    /// </summary>
    Task<IReadOnlyList<Permission>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by category.
    /// </summary>
    Task<IReadOnlyList<Permission>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by IDs.
    /// </summary>
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new permission.
    /// </summary>
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    void Update(Permission permission);

    /// <summary>
    /// Deletes a permission.
    /// </summary>
    void Delete(Permission permission);

    /// <summary>
    /// Checks if a permission code is already taken.
    /// </summary>
    Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default);
}
