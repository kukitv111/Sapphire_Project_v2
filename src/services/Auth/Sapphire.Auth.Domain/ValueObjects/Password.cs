using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Domain.ValueObjects;

/// <summary>
/// Password value object — immutable container for password hash and salt.
/// Does NOT perform hashing — that's the responsibility of IPasswordHasher in Application layer.
/// </summary>
public sealed record Password : ValueObject
{
    /// <summary>
    /// The hashed password.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// The salt used for hashing.
    /// </summary>
    public string Salt { get; }

    private Password(string hash, string salt)
    {
        Hash = hash;
        Salt = salt;
    }

    /// <summary>
    /// Minimum password length for validation (used by Application layer).
    /// </summary>
    public const int MinLength = 8;

    /// <summary>
    /// Maximum password length for validation.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Creates a Password from hash and salt (prepared by IPasswordHasher).
    /// This is the ONLY way to create a Password in Domain layer.
    /// </summary>
    public static Password FromHash(string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash cannot be empty", nameof(hash));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Password salt cannot be empty", nameof(salt));

        return new Password(hash, salt);
    }

    /// <summary>
    /// Validates a plain text password against complexity rules.
    /// Called from Application layer before hashing.
    /// </summary>
    public static void ValidatePlainText(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        if (password.Length < MinLength)
            throw new ArgumentException($"Password must be at least {MinLength} characters", nameof(password));

        if (password.Length > MaxLength)
            throw new ArgumentException($"Password cannot exceed {MaxLength} characters", nameof(password));

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);

        if (!hasUpper || !hasLower || !hasDigit)
            throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, and one digit", nameof(password));
    }

    /// <summary>
    /// Checks if a plain text password meets complexity requirements.
    /// </summary>
    public static bool IsValidPlainText(string password)
    {
        try
        {
            ValidatePlainText(password);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => "********";
}
