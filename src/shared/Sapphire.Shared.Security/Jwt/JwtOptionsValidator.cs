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
            throw new InvalidOperationException("JWT secret key is missing or empty");

        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            && KnownDevelopmentSecrets.Contains(options.SecretKey))
        {
            throw new InvalidOperationException("JWT secret key uses a known development/default value");
        }
    }
}
