using Sapphire.Shared.Kernel.Events;

namespace Sapphire.Auth.Domain.Events;

/// <summary>
/// Domain event raised when a new user registers.
/// </summary>
public sealed record UserRegisteredEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid? BranchId { get; init; }
    public DateTime RegisteredAt { get; init; }
}

/// <summary>
/// Domain event raised when a user successfully logs in.
/// </summary>
public sealed record UserLoggedInEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string AuthMethod { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
    public DateTime LoggedInAt { get; init; }
}

/// <summary>
/// Domain event raised when a user logs out.
/// </summary>
public sealed record UserLoggedOutEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime LoggedOutAt { get; init; }
}

/// <summary>
/// Domain event raised when a user changes their password.
/// </summary>
public sealed record PasswordChangedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = string.Empty; // UserId of who changed it
}

/// <summary>
/// Domain event raised when a user is banned.
/// </summary>
public sealed record UserBannedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public Guid BannedBy { get; init; }
    public DateTime BannedAt { get; init; }
}

/// <summary>
/// Domain event raised when a user is unbanned.
/// </summary>
public sealed record UserUnbannedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public Guid UnbannedBy { get; init; }
    public DateTime UnbannedAt { get; init; }
}

/// <summary>
/// Domain event raised when a role is assigned to a user.
/// </summary>
public sealed record RoleAssignedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public Guid AssignedBy { get; init; }
    public DateTime AssignedAt { get; init; }
}

/// <summary>
/// Domain event raised when a role is removed from a user.
/// </summary>
public sealed record RoleRemovedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public Guid RemovedBy { get; init; }
    public DateTime RemovedAt { get; init; }
}

/// <summary>
/// Domain event raised when a refresh token is created.
/// </summary>
public sealed record RefreshTokenCreatedEvent : DomainEventBase
{
    public Guid TokenId { get; init; }
    public Guid UserId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
}

/// <summary>
/// Domain event raised when a refresh token is revoked.
/// </summary>
public sealed record RefreshTokenRevokedEvent : DomainEventBase
{
    public Guid TokenId { get; init; }
    public Guid UserId { get; init; }
    public string? Reason { get; init; }
    public DateTime RevokedAt { get; init; }
}
