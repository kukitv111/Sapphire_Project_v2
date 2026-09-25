using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Shared.Security.Jwt;

namespace Sapphire.Auth.Infrastructure.Security;

/// <summary>
/// Implementation of ITokenService using Shared.Security.Jwt.TokenService.
/// Loads role names from database for proper RBAC token generation.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly TokenService _tokenService;
    private readonly AuthDbContext _dbContext;

    public JwtTokenService(TokenService tokenService, AuthDbContext dbContext)
    {
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    public async Task<TokenDto> GenerateTokensAsync(User user, string? deviceInfo = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var roleNames = await GetRoleNamesAsync(user, cancellationToken);
        var permissions = await GetPermissionsAsync(user, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email.Value,
            roleNames,
            permissions, user.TokenVersion);

        var (refreshToken, expiresAt) = _tokenService.GenerateRefreshToken();

        // Create refresh token entity and attach to user
        var tokenHash = HashRefreshToken(refreshToken);
        var refreshTokenEntity = user.CreateRefreshToken(tokenHash, expiresAt, deviceInfo, ipAddress);

        var tokenDto = new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenId = refreshTokenEntity.Id,
            AccessTokenExpiresAt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(accessToken).ValidTo,
            RefreshTokenExpiresAt = expiresAt
        };

        return tokenDto;
    }

    public string GenerateAccessToken(User user)
    {
        var roleNames = GetRoleNames(user);
        var permissions = GetPermissions(user);
        return _tokenService.GenerateAccessToken(user.Id, user.Email.Value, roleNames, permissions,
            user.TokenVersion);
    }

    public DateTime GetAccessTokenExpiresAt(string accessToken) =>
        new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken).ValidTo;

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

    private async Task<IReadOnlyList<string>> GetRoleNamesAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user.Roles.Count == 0)
            return [];

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        return await _dbContext.Roles
            .Where(r => r.IsActive && roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    private IReadOnlyList<string> GetRoleNames(User user)
    {
        if (user.Roles.Count == 0)
            return [];

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        return _dbContext.Roles
            .Where(r => r.IsActive && roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> GetPermissionsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (user.Roles.Count == 0)
            return [];

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        return await _dbContext.Roles
            .Where(r => r.IsActive && roleIds.Contains(r.Id))
            .SelectMany(r => r.Permissions)
            .Where(p => p.IsActive)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private IReadOnlyList<string> GetPermissions(User user)
    {
        if (user.Roles.Count == 0)
            return [];

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        return _dbContext.Roles
            .Where(r => r.IsActive && roleIds.Contains(r.Id))
            .SelectMany(r => r.Permissions)
            .Where(p => p.IsActive)
            .Select(p => p.Code)
            .Distinct()
            .ToList();
    }
}

