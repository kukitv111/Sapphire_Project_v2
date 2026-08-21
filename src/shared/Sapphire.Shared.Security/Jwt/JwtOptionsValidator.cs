namespace Sapphire.Shared.Security.Jwt;

/// <summary>
/// Validates JWT configuration before token services are used.
/// </summary>
public static class JwtOptionsValidator
{
    private static readonly HashSet<string> KnownDevelopmentSecrets = new(StringComparer.Ordinal)
    {
        "sapphire-dev-secret-key-change-me-in-production-32chars-min",
        "change-me-in-production!!!",
        "super-secret-key-must-be-at-least-32-chars!",
        "dev-secret-32-chars-placeholder-ok!!"
    };

    public static void Validate(JwtOptions? options, string environmentName)
    {
        if (options is null)
            throw new InvalidOperationException("JWT configuration is missing");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new InvalidOperationException("JWT SecretKey is missing or empty");

        if (options.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long for HMAC signing");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException("JWT Issuer is missing or empty");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException("JWT Audience is missing or empty");

        if (options.AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT AccessTokenExpirationMinutes must be greater than zero");

        if (options.RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("JWT RefreshTokenExpirationDays must be greater than zero");

        if (options.UseRsa)
            throw new NotSupportedException("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");

        if (options.UseRsa)
            throw new NotSupportedException("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");

        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            && KnownDevelopmentSecrets.Contains(options.SecretKey))
        {
            throw new InvalidOperationException("JWT secret key uses a known development/default value in Production environment");
        }
    }
}
