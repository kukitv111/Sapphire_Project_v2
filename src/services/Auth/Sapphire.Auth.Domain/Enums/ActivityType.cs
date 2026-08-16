namespace Sapphire.Auth.Domain.Enums;

/// <summary>
/// Represents the type of activity recorded in history.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// User successfully logged in.
    /// </summary>
    Login = 1,

    /// <summary>
    /// User logged out.
    /// </summary>
    Logout = 2,

    /// <summary>
    /// User registered a new account.
    /// </summary>
    Register = 3,

    /// <summary>
    /// User changed password.
    /// </summary>
    PasswordChange = 4,

    /// <summary>
    /// User account was banned.
    /// </summary>
    Ban = 5,

    /// <summary>
    /// User account was unbanned.
    /// </summary>
    Unban = 6,

    /// <summary>
    /// User refreshed authentication token.
    /// </summary>
    TokenRefresh = 7,

    /// <summary>
    /// User's role was changed.
    /// </summary>
    RoleChange = 8,

    /// <summary>
    /// User profile was updated.
    /// </summary>
    ProfileUpdate = 9,

    /// <summary>
    /// Failed login attempt.
    /// </summary>
    FailedLogin = 10
}
