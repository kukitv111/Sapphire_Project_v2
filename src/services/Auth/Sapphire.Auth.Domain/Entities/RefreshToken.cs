using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Refresh token entity for JWT refresh token rotation.
/// Supports token revocation and device tracking.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public Guid FamilyId { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresAt, string? deviceInfo, string? ipAddress, Guid? familyId = null) : base()
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        FamilyId = familyId ?? Guid.NewGuid();
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt, string? deviceInfo = null, string? ipAddress = null, Guid? familyId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty", nameof(tokenHash));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        return new RefreshToken(userId, tokenHash, expiresAt, deviceInfo, ipAddress, familyId);
    }

    /// <summary>
    /// Revokes the token.
    /// </summary>
    public void Revoke(string? reason = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this token as replaced by a new token.
    /// Used for token rotation detection.
    /// </summary>
    public void MarkAsReplaced(Guid newTokenId, string? reason = null)
    {
        Revoke(reason ?? "Token rotated");
        ReplacedByTokenId = newTokenId;
    }

    /// <summary>
    /// Extends the expiration date.
    /// Only allowed if token is still active.
    /// </summary>
    public void ExtendExpiration(DateTime newExpiresAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot extend expired or revoked token");

        if (newExpiresAt <= ExpiresAt)
            throw new ArgumentException("New expiration must be later than current", nameof(newExpiresAt));

        ExpiresAt = newExpiresAt;
        UpdatedAt = DateTime.UtcNow;
    }
}
