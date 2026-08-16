namespace Sapphire.Auth.Domain.Enums;

/// <summary>
/// Represents the authentication method used for login.
/// </summary>
public enum AuthMethod
{
    /// <summary>
    /// Username and password authentication.
    /// </summary>
    Password = 1,

    /// <summary>
    /// RFID card authentication.
    /// </summary>
    Card = 2,

    /// <summary>
    /// Phone number with OTP authentication.
    /// </summary>
    Phone = 3,

    /// <summary>
    /// Email with magic link or OTP.
    /// </summary>
    Email = 4,

    /// <summary>
    /// Refresh token authentication.
    /// </summary>
    RefreshToken = 5
}
