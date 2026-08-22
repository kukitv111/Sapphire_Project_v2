namespace Sapphire.Auth.Application.Interfaces.Security;

/// <summary>
/// Interface for password hashing and verification.
/// Defined in Auth Application so handlers can depend on an application port without Infrastructure.
/// Implementation lives in Auth Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    /// <param name="plainPassword">The plain text password to hash.</param>
    /// <returns>A tuple containing (hash, salt).</returns>
    (string Hash, string Salt) HashPassword(string plainPassword);

    /// <summary>
    /// Verifies a plain text password against stored hash and salt.
    /// </summary>
    /// <param name="plainPassword">The plain text password to verify.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <param name="salt">The stored salt.</param>
    /// <returns>True if the password matches.</returns>
    bool VerifyPassword(string plainPassword, string hash, string salt);

    /// <summary>
    /// Checks if a hash needs to be rehashed (e.g., iterations increased).
    /// </summary>
    bool NeedsRehash(string hash);
}
