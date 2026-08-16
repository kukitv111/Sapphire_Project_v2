using System.Security.Cryptography;
using System.Text.Json.Serialization;

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
    /// Whether to use RSA signing instead of HMAC.
    /// </summary>
    public bool UseRsa { get; set; } = false;

    /// <summary>
    /// RSA private key PEM (if UseRsa is true).
    /// </summary>
    public string? RsaPrivateKeyPem { get; set; }

    /// <summary>
    /// RSA public key PEM (if UseRsa is true).
    /// </summary>
    public string? RsaPublicKeyPem { get; set; }

    /// <summary>
    /// Gets RSA parameters from PEM-encoded private key.
    /// </summary>
    public RSAParameters? GetRsaParameters()
    {
        if (!UseRsa || string.IsNullOrEmpty(RsaPrivateKeyPem))
            return null;

        var rsa = RSA.Create();
        rsa.ImportFromPem(RsaPrivateKeyPem);
        return rsa.ExportParameters(true);
    }

    /// <summary>
    /// Gets RSA public key parameters.
    /// </summary>
    public RSAParameters? GetRsaPublicKeyParameters()
    {
        if (!UseRsa || string.IsNullOrEmpty(RsaPublicKeyPem))
            return null;

        var rsa = RSA.Create();
        rsa.ImportFromPem(RsaPublicKeyPem);
        return rsa.ExportParameters(false);
    }
}
