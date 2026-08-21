using System.Security.Cryptography;
using System.Text;
using Sapphire.Auth.Application.Interfaces.Security;

namespace Sapphire.Auth.Infrastructure.Security;

/// <summary>
/// PBKDF2 implementation of IPasswordHasher.
/// Uses 100,000 iterations with HMAC-SHA256.
/// Output format: iterations;base64(salt);base64(hash)
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32; // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    private const char Delimiter = ';';

    /// <inheritdoc />
    public (string Hash, string Salt) HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            saltBytes,
            Iterations,
            HashAlgorithm,
            KeySize);

        var salt = Convert.ToBase64String(saltBytes);
        var hash = $"{Iterations}{Delimiter}{salt}{Delimiter}{Convert.ToBase64String(hashBytes)}";

        return (hash, salt);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string plainPassword, string hash, string salt)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

        if (string.IsNullOrEmpty(hash))
            return false;

        var parts = hash.Split(Delimiter);
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var iterations))
            return false;

        var storedSalt = Convert.FromBase64String(parts[1]);
        var storedHash = Convert.FromBase64String(parts[2]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            storedSalt,
            iterations,
            HashAlgorithm,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    /// <inheritdoc />
    public bool NeedsRehash(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return true;

        var parts = hash.Split(Delimiter);
        if (parts.Length != 3)
            return true;

        if (!int.TryParse(parts[0], out var iterations))
            return true;

        return iterations < Iterations;
    }
}
