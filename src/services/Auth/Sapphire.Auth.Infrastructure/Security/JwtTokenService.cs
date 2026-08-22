using System.Security.Cryptography;
using System.Text;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.Auth.Infrastructure.Security;

/// <summary>
/// Implementation of ITokenService using Shared.Security.Jwt.TokenService.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly TokenService _tokenService;

    public JwtTokenService(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public Task<TokenDto> GenerateTokensAsync(User user, string? deviceInfo = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var roles = user.Roles.Select(r => r.RoleId.ToString());
        var permissions = new List<string>(); // loaded from Role.Permissions when available

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email.Value,
            roles,
            permissions);

        var (refreshToken, expiresAt) = _tokenService.GenerateRefreshToken();

        // Create refresh token entity and attach to user
        var tokenHash = HashRefreshToken(refreshToken);
        var refreshTokenEntity = user.CreateRefreshToken(tokenHash, expiresAt, deviceInfo, ipAddress);

        var tokenDto = new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenId = refreshTokenEntity.Id,
            ExpiresAt = expiresAt
        };

        return Task.FromResult(tokenDto);
    }

    public string GenerateAccessToken(User user)
    {
        var roles = user.Roles.Select(r => r.RoleId.ToString());
        var permissions = new List<string>();
        return _tokenService.GenerateAccessToken(user.Id, user.Email.Value, roles, permissions);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        return _tokenService.GenerateRefreshToken();
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes);
    }

    public Guid? GetUserIdFromToken(string accessToken)
    {
        return _tokenService.GetUserIdFromToken(accessToken);
    }
}

