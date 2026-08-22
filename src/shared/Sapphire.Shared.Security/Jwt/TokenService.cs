using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Sapphire.Shared.Security.Jwt;

/// <summary>
/// JWT token service for generating and validating tokens.
/// </summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _symmetricKey;

    public TokenService(JwtOptions options, string environmentName)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        JwtOptionsValidator.Validate(options, environmentName);
        _symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
    }

    /// <summary>
    /// Generates an access token for a user.
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var signingCredentials = new SigningCredentials(_symmetricKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        return (token, expiresAt);
    }

    /// <summary>
    /// Gets the validation parameters for token validation.
    /// </summary>
    public TokenValidationParameters GetValidationParameters()
    {
        return JwtAuthenticationExtensions.GetTokenValidationParameters(_options);
    }

    /// <summary>
    /// Validates a token and extracts the principal.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, GetValidationParameters(), out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts user ID from a token without full validation.
    /// </summary>
    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            return subClaim != null ? Guid.Parse(subClaim.Value) : null;
        }
        catch
        {
            return null;
        }
    }
}
