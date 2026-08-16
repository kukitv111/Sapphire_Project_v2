using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for User aggregate.
/// Follows Repository pattern from DDD.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by phone number.
    /// </summary>
    Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username or email (for login).
    /// </summary>
    Task<User?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is already taken.
    /// </summary>
    Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already registered.
    /// </summary>
    Task<bool> IsEmailRegisteredAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a phone number is already registered.
    /// </summary>
    Task<bool> IsPhoneRegisteredAsync(PhoneNumber phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    void Update(User user);

    /// <summary>
    /// Deletes a user (soft delete).
    /// </summary>
    void Delete(User user);

    /// <summary>
    /// Gets users by branch ID.
    /// </summary>
    Task<IReadOnlyList<User>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users.
    /// </summary>
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role ID.
    /// </summary>
    Task<IReadOnlyList<User>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
}
