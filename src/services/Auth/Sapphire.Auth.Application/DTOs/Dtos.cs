namespace Sapphire.Auth.Application.DTOs;

/// <summary>
/// Data transfer object for user information.
/// </summary>
public sealed record UserDto
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
    public IReadOnlyList<RoleDto> Roles { get; init; } = [];
}

/// <summary>
/// Data transfer object for authentication result.
/// </summary>
public sealed record AuthResultDto
{
    public UserDto User { get; init; } = null!;
    public TokenDto Tokens { get; init; } = null!;
}

/// <summary>
/// Data transfer object for JWT tokens.
/// </summary>
public sealed record TokenDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public Guid RefreshTokenId { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Data transfer object for refresh token.
/// </summary>
public sealed record RefreshTokenDto
{
    public Guid Id { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; init; }
    public string? DeviceInfo { get; init; }
}

/// <summary>
/// Data transfer object for role information.
/// </summary>
public sealed record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
}

