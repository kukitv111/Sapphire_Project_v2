using Sapphire.Auth.Application.DTOs;

namespace Sapphire.Auth.Api.Contracts;

/// <summary>
/// Registration request contract.
/// </summary>
public sealed record RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid? BranchId { get; init; }
    public string? DeviceInfo { get; init; }
}

/// <summary>
/// Login request contract. Login is either username or email.
/// </summary>
public sealed record LoginRequest
{
    public string Login { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
}

/// <summary>
/// Refresh token request contract.
/// </summary>
public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
}

/// <summary>
/// Logout request contract.
/// </summary>
public sealed record LogoutRequest
{
    public string? RefreshToken { get; init; }
    public bool RevokeAll { get; init; }
}

/// <summary>
/// Change password request contract.
/// </summary>
public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Authentication response contract (flat, stable API surface).
/// </summary>
public sealed record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserResponse User { get; init; } = null!;

    public static AuthResponse From(AuthResultDto result) => new()
    {
        AccessToken = result.Tokens.AccessToken,
        RefreshToken = result.Tokens.RefreshToken,
        ExpiresAt = result.Tokens.ExpiresAt,
        User = UserResponse.From(result.User)
    };
}

/// <summary>
/// User response contract.
/// </summary>
public sealed record UserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Guid? BranchId { get; init; }
    public decimal BonusBalance { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsBanned { get; init; }
    public string? BanReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<RoleResponse> Roles { get; init; } = [];

    public static UserResponse From(UserDto dto) => new()
    {
        Id = dto.Id,
        Username = dto.Username,
        Email = dto.Email,
        Phone = dto.Phone,
        BranchId = dto.BranchId,
        BonusBalance = dto.BonusBalance,
        Status = dto.Status,
        IsBanned = dto.IsBanned,
        BanReason = dto.BanReason,
        CreatedAt = dto.CreatedAt,
        LastLoginAt = dto.LastLoginAt,
        Roles = dto.Roles.Select(RoleResponse.From).ToArray()
    };
}

/// <summary>
/// Role response contract.
/// </summary>
public sealed record RoleResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }

    public static RoleResponse From(RoleDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        IsSystem = dto.IsSystem
    };
}
