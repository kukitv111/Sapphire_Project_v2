namespace Sapphire.Auth.Domain.Enums;

/// <summary>
/// Represents the status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is active and can authenticate.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User account is temporarily suspended.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// User account is permanently banned.
    /// </summary>
    Banned = 3,

    /// <summary>
    /// User account is pending activation (e.g., email verification).
    /// </summary>
    PendingActivation = 4,

    /// <summary>
    /// User account has been deleted (soft delete).
    /// </summary>
    Deleted = 5
}
