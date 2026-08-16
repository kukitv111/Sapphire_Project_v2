using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for RefreshToken entity.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by ID.
    /// </summary>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by token hash.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active refresh tokens for a user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token.
    /// </summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refresh token.
    /// </summary>
    void Update(RefreshToken refreshToken);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired tokens.
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}
