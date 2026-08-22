namespace Sapphire.Shared.Security.Jwt;

/// <summary>
/// JWT configuration options.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing tokens. Minimum 32 characters for HS256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;

    /// <summary>
    /// RSA signing is reserved for a future implementation and is rejected by the current HMAC-only MVP policy.
    /// </summary>
    public bool UseRsa { get; set; } = false;

    /// <summary>Reserved for a future RSA implementation; not used by the current policy.</summary>
    public string? RsaPrivateKeyPem { get; set; } = null;

    /// <summary>Reserved for a future RSA implementation; not used by the current policy.</summary>
    public string? RsaPublicKeyPem { get; set; } = null;

}

