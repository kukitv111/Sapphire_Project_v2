using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Aggregates;

namespace Sapphire.Auth.Application.Interfaces;

/// <summary>
/// Application-level token service abstraction.
/// Infrastructure provides implementation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates access and refresh tokens for a user.
    /// </summary>
    Task<TokenDto> GenerateTokensAsync(User user, string? deviceInfo = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an access token without creating a refresh token.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates an opaque refresh token without persisting it.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();

    /// <summary>
    /// Hashes a refresh token for secure storage.
    /// </summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Validates an access token and returns user id.
    /// </summary>
    Guid? GetUserIdFromToken(string accessToken);
}


