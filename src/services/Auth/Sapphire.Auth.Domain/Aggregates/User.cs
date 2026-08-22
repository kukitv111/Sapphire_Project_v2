using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.Enums;
using Sapphire.Auth.Domain.Events;
using Sapphire.Auth.Domain.Exceptions;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Domain.Aggregates;

/// <summary>
/// User aggregate root representing a user in the system.
/// Manages authentication, authorization, and user lifecycle.
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    public Username Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public PhoneNumber? Phone { get; private set; }
    public Password Password { get; private set; } = null!;
    public Guid? BranchId { get; private set; }
    public long BonusBalanceCents { get; private set; }
    public UserStatus Status { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? BannedAt { get; private set; }
    public Guid? BannedBy { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    private User(
        Username username,
        Email email,
        Password password,
        PhoneNumber? phone,
        Guid? branchId) : base()
    {
        Username = username;
        Email = email;
        Password = password;
        Phone = phone;
        BranchId = branchId;
        Status = UserStatus.Active;
        BonusBalanceCents = 0;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a new user. Password must be pre-hashed by Application layer via IPasswordHasher.
    /// </summary>
    public static User Create(
        string username,
        string email,
        Password hashedPassword,
        string? phone = null,
        Guid? branchId = null)
    {
        var usernameVo = Username.From(username);
        var emailVo = Email.From(email);
        var phoneVo = phone != null ? PhoneNumber.From(phone) : null;

        var user = new User(usernameVo, emailVo, hashedPassword, phoneVo, branchId);

        user.AddDomainEvent(new UserRegisteredEvent
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone?.Value,
            BranchId = user.BranchId,
            RegisteredAt = user.CreatedAt
        });

        return user;
    }

    /// <summary>
    /// Creates a user with a specific ID (for seeding/testing).
    /// </summary>
    public static User CreateWithId(
        Guid id,
        string username,
        string email,
        string passwordHash,
        string passwordSalt,
        string? phone = null,
        Guid? branchId = null)
    {
        var usernameVo = Username.From(username);
        var emailVo = Email.From(email);
        var passwordVo = Password.FromHash(passwordHash, passwordSalt);
        var phoneVo = phone != null ? PhoneNumber.From(phone) : null;

        var user = new User(usernameVo, emailVo, passwordVo, phoneVo, branchId)
        {
            Id = id
        };

        return user;
    }

    #endregion

    #region Authentication

    /// <summary>
    /// Verifies password against stored hash.
    /// Password verification is delegated to IPasswordHasher in Application layer.
    /// This method only updates internal state based on result.
    /// </summary>
    public void RecordAuthenticationAttempt(bool passwordValid)
    {
        if (Status != UserStatus.Active)
            return;

        if (passwordValid)
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }
        else
        {
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= 5)
            {
                LockedUntil = DateTime.UtcNow.AddMinutes(15);
            }
        }
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin(string ipAddress, string? deviceInfo = null)
    {
        EnsureIsActive();

        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserLoggedInEvent
        {
            UserId = Id,
            Username = Username,
            AuthMethod = "Password",
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            LoggedInAt = LastLoginAt.Value
        });
    }

    /// <summary>
    /// Changes the user's password.
    /// Password must be pre-hashed by Application layer via IPasswordHasher.
    /// </summary>
    public void ChangePassword(Password newHashedPassword, Guid changedBy)
    {
        EnsureIsActive();

        Password = newHashedPassword;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PasswordChangedEvent
        {
            UserId = Id,
            Username = Username,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy.ToString()
        });
    }

    /// <summary>
    /// Forces a password change (admin action).
    /// Password must be pre-hashed by Application layer via IPasswordHasher.
    /// </summary>
    public void ForceChangePassword(Password newHashedPassword, Guid changedBy)
    {
        Password = newHashedPassword;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PasswordChangedEvent
        {
            UserId = Id,
            Username = Username,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy.ToString()
        });
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Assigns a role to the user.
    /// </summary>
    public void AssignRole(Role role, Guid assignedBy)
    {
        EnsureIsActive();
        ArgumentNullException.ThrowIfNull(role);

        if (!role.IsActive)
            throw new ArgumentException("Cannot assign inactive role", nameof(role));

        if (HasRole(role.Id))
            return;

        var userRole = UserRole.Create(role.Id, assignedBy);
        _roles.Add(userRole);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new RoleAssignedEvent
        {
            UserId = Id,
            RoleId = role.Id,
            RoleName = role.Name,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    public void RemoveRole(Guid roleId, Guid removedBy)
    {
        var userRole = _roles.FirstOrDefault(r => r.RoleId == roleId);
        if (userRole == null)
            return;

        _roles.Remove(userRole);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new RoleRemovedEvent
        {
            UserId = Id,
            RoleId = roleId,
            RoleName = string.Empty,
            RemovedBy = removedBy,
            RemovedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    public bool HasRole(Guid roleId) => _roles.Any(r => r.RoleId == roleId);

    #endregion

    #region Refresh Tokens

    /// <summary>
    /// Creates a new refresh token for the user.
    /// </summary>
    public RefreshToken CreateRefreshToken(string tokenHash, DateTime expiresAt, string? deviceInfo = null, string? ipAddress = null, Guid? familyId = null)
    {
        EnsureIsActive();

        var refreshToken = RefreshToken.Create(Id, tokenHash, expiresAt, deviceInfo, ipAddress, familyId);
        _refreshTokens.Add(refreshToken);

        AddDomainEvent(new RefreshTokenCreatedEvent
        {
            TokenId = refreshToken.Id,
            UserId = Id,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        });

        return refreshToken;
    }

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    public void RevokeRefreshToken(Guid tokenId, string? reason = null)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
        if (token == null || !token.IsActive)
            return;

        token.Revoke(reason);

        AddDomainEvent(new RefreshTokenRevokedEvent
        {
            TokenId = tokenId,
            UserId = Id,
            Reason = reason,
            RevokedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Revokes all refresh tokens for the user.
    /// </summary>
    public void RevokeAllRefreshTokens(string? reason = null)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke(reason ?? "All tokens revoked");
        }
    }

    /// <summary>
    /// Gets an active refresh token by ID.
    /// </summary>
    public RefreshToken? GetActiveRefreshToken(Guid tokenId)
    {
        return _refreshTokens.FirstOrDefault(t => t.Id == tokenId && t.IsActive);
    }

    /// <summary>
    /// Cleans up expired refresh tokens.
    /// </summary>
    public void CleanupExpiredTokens()
    {
        _refreshTokens.RemoveAll(t => t.IsExpired && !t.IsRevoked);
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Bans the user.
    /// </summary>
    public void Ban(string reason, Guid bannedBy)
    {
        if (Status == UserStatus.Banned)
            return;

        Status = UserStatus.Banned;
        BanReason = reason;
        BannedAt = DateTime.UtcNow;
        BannedBy = bannedBy;
        UpdatedAt = DateTime.UtcNow;

        RevokeAllRefreshTokens("User banned");

        AddDomainEvent(new UserBannedEvent
        {
            UserId = Id,
            Username = Username,
            Reason = reason,
            BannedBy = bannedBy,
            BannedAt = BannedAt.Value
        });
    }

    /// <summary>
    /// Unbans the user.
    /// </summary>
    public void Unban(Guid unbannedBy)
    {
        if (Status != UserStatus.Banned)
            return;

        Status = UserStatus.Active;
        BanReason = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserUnbannedEvent
        {
            UserId = Id,
            Username = Username,
            UnbannedBy = unbannedBy,
            UnbannedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Suspends the user.
    /// </summary>
    public void Suspend()
    {
        if (Status == UserStatus.Banned)
            throw new InvalidOperationException("Cannot suspend a banned user");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user.
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.Banned)
            throw new InvalidOperationException("Cannot activate a banned user. Unban first.");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft deletes the user.
    /// </summary>
    public void Delete()
    {
        Status = UserStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
        RevokeAllRefreshTokens("User deleted");
    }

    #endregion

    #region Profile Updates

    /// <summary>
    /// Updates the user's email.
    /// </summary>
    public void UpdateEmail(Email email)
    {
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's phone number.
    /// </summary>
    public void UpdatePhone(PhoneNumber? phone)
    {
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's branch.
    /// </summary>
    public void UpdateBranch(Guid? branchId)
    {
        BranchId = branchId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds bonus balance to the user.
    /// </summary>
    public void AddBonus(long cents)
    {
        if (cents < 0)
            throw new ArgumentException("Bonus amount cannot be negative", nameof(cents));

        BonusBalanceCents += cents;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deducts bonus balance from the user.
    /// </summary>
    public void DeductBonus(long cents)
    {
        if (cents < 0)
            throw new ArgumentException("Bonus amount cannot be negative", nameof(cents));

        if (cents > BonusBalanceCents)
            throw new InvalidOperationException("Insufficient bonus balance");

        BonusBalanceCents -= cents;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Business Rules

    /// <summary>
    /// Checks if the user can log in.
    /// </summary>
    public bool CanLogin()
    {
        if (Status == UserStatus.Banned)
            return false;

        if (Status == UserStatus.Suspended)
            return false;

        if (Status == UserStatus.Deleted)
            return false;

        if (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the user is locked out.
    /// </summary>
    public bool IsLockedOut()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Unlocks the user account.
    /// </summary>
    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Private Helpers

    private void EnsureIsActive()
    {
        if (Status != UserStatus.Active)
            throw new UserBannedException(BanReason ?? "User account is not active");
    }

    #endregion
}
