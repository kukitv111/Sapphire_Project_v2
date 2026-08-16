using Sapphire.Shared.Kernel.Exceptions;

namespace Sapphire.Auth.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is not found.
/// </summary>
public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) 
        : base("USER_NOT_FOUND", $"User with ID '{userId}' was not found")
    {
    }

    public UserNotFoundException(string identifier) 
        : base("USER_NOT_FOUND", $"User with identifier '{identifier}' was not found")
    {
    }
}

/// <summary>
/// Exception thrown when credentials are invalid.
/// </summary>
public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("INVALID_CREDENTIALS", "Invalid username or password")
    {
    }
}

/// <summary>
/// Exception thrown when a user is banned.
/// </summary>
public sealed class UserBannedException : DomainException
{
    public string BanReason { get; }

    public UserBannedException(string reason) 
        : base("USER_BANNED", $"User account is banned. Reason: {reason}")
    {
        BanReason = reason;
    }
}

/// <summary>
/// Exception thrown when a user is suspended.
/// </summary>
public sealed class UserSuspendedException : DomainException
{
    public UserSuspendedException() 
        : base("USER_SUSPENDED", "User account is suspended")
    {
    }
}

/// <summary>
/// Exception thrown when a username is already taken.
/// </summary>
public sealed class UsernameAlreadyTakenException : DomainException
{
    public UsernameAlreadyTakenException(string username) 
        : base("USERNAME_TAKEN", $"Username '{username}' is already taken")
    {
    }
}

/// <summary>
/// Exception thrown when an email is already registered.
/// </summary>
public sealed class EmailAlreadyRegisteredException : DomainException
{
    public EmailAlreadyRegisteredException(string email) 
        : base("EMAIL_REGISTERED", $"Email '{email}' is already registered")
    {
    }
}

/// <summary>
/// Exception thrown when a phone number is already registered.
/// </summary>
public sealed class PhoneAlreadyRegisteredException : DomainException
{
    public PhoneAlreadyRegisteredException(string phone) 
        : base("PHONE_REGISTERED", $"Phone number '{phone}' is already registered")
    {
    }
}

/// <summary>
/// Exception thrown when a refresh token is invalid or expired.
/// </summary>
public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException() 
        : base("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired")
    {
    }

    public InvalidRefreshTokenException(string reason) 
        : base("INVALID_REFRESH_TOKEN", $"Refresh token is invalid: {reason}")
    {
    }
}

/// <summary>
/// Exception thrown when a role is not found.
/// </summary>
public sealed class RoleNotFoundException : DomainException
{
    public RoleNotFoundException(Guid roleId) 
        : base("ROLE_NOT_FOUND", $"Role with ID '{roleId}' was not found")
    {
    }

    public RoleNotFoundException(string roleName) 
        : base("ROLE_NOT_FOUND", $"Role '{roleName}' was not found")
    {
    }
}

/// <summary>
/// Exception thrown when a permission is not found.
/// </summary>
public sealed class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(string code) 
        : base("PERMISSION_NOT_FOUND", $"Permission '{code}' was not found")
    {
    }
}

/// <summary>
/// Exception thrown when user cannot perform an action due to insufficient permissions.
/// </summary>
public sealed class InsufficientPermissionsException : DomainException
{
    public InsufficientPermissionsException(string permission) 
        : base("INSUFFICIENT_PERMISSIONS", $"User does not have permission '{permission}'")
    {
    }
}
