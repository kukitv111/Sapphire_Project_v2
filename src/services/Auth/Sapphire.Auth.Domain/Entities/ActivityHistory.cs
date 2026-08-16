using Sapphire.Auth.Domain.Enums;
using Sapphire.Shared.Kernel.Entities;

namespace Sapphire.Auth.Domain.Entities;

/// <summary>
/// Activity history entity for auditing user actions.
/// Immutable record of all user activities.
/// </summary>
public sealed class ActivityHistory : Entity
{
    public Guid UserId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Description { get; private set; }
    public string? Metadata { get; private set; }
    public string? IpAddress { get; private set; }
    public string? DeviceInfo { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private ActivityHistory(
        Guid userId,
        ActivityType activityType,
        string? entityType,
        Guid? entityId,
        string description,
        string? metadata,
        string? ipAddress,
        string? deviceInfo) : base()
    {
        UserId = userId;
        ActivityType = activityType;
        EntityType = entityType;
        EntityId = entityId;
        Description = description;
        Metadata = metadata;
        IpAddress = ipAddress;
        DeviceInfo = deviceInfo;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new activity history record.
    /// </summary>
    public static ActivityHistory Create(
        Guid userId,
        ActivityType activityType,
        string? description = null,
        string? entityType = null,
        Guid? entityId = null,
        string? metadata = null,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var descriptionText = description ?? GetDefaultDescription(activityType);

        return new ActivityHistory(
            userId,
            activityType,
            entityType,
            entityId,
            descriptionText,
            metadata,
            ipAddress,
            deviceInfo);
    }

    /// <summary>
    /// Creates a login activity record.
    /// </summary>
    public static ActivityHistory Login(Guid userId, string? ipAddress = null, string? deviceInfo = null, bool success = true)
    {
        return Create(
            userId,
            success ? ActivityType.Login : ActivityType.FailedLogin,
            success ? "User logged in successfully" : "Failed login attempt",
            ipAddress: ipAddress,
            deviceInfo: deviceInfo);
    }

    /// <summary>
    /// Creates a logout activity record.
    /// </summary>
    public static ActivityHistory Logout(Guid userId, string? ipAddress = null, string? deviceInfo = null)
    {
        return Create(
            userId,
            ActivityType.Logout,
            "User logged out",
            ipAddress: ipAddress,
            deviceInfo: deviceInfo);
    }

    /// <summary>
    /// Creates a registration activity record.
    /// </summary>
    public static ActivityHistory Register(Guid userId, string? ipAddress = null, string? deviceInfo = null)
    {
        return Create(
            userId,
            ActivityType.Register,
            "User registered",
            ipAddress: ipAddress,
            deviceInfo: deviceInfo);
    }

    /// <summary>
    /// Creates a password change activity record.
    /// </summary>
    public static ActivityHistory PasswordChange(Guid userId, string? ipAddress = null, string? deviceInfo = null)
    {
        return Create(
            userId,
            ActivityType.PasswordChange,
            "Password changed",
            ipAddress: ipAddress,
            deviceInfo: deviceInfo);
    }

    /// <summary>
    /// Creates a ban activity record.
    /// </summary>
    public static ActivityHistory Ban(Guid userId, Guid bannedBy, string reason)
    {
        return Create(
            userId,
            ActivityType.Ban,
            $"User banned. Reason: {reason}",
            entityType: "User",
            entityId: userId,
            metadata: $"{{\"bannedBy\": \"{bannedBy}\", \"reason\": \"{reason}\"}}");
    }

    /// <summary>
    /// Creates an unban activity record.
    /// </summary>
    public static ActivityHistory Unban(Guid userId, Guid unbannedBy)
    {
        return Create(
            userId,
            ActivityType.Unban,
            "User unbanned",
            entityType: "User",
            entityId: userId,
            metadata: $"{{\"unbannedBy\": \"{unbannedBy}\"}}");
    }

    private static string GetDefaultDescription(ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.Login => "User logged in",
            ActivityType.Logout => "User logged out",
            ActivityType.Register => "User registered",
            ActivityType.PasswordChange => "Password changed",
            ActivityType.Ban => "User banned",
            ActivityType.Unban => "User unbanned",
            ActivityType.TokenRefresh => "Token refreshed",
            ActivityType.RoleChange => "Role changed",
            ActivityType.ProfileUpdate => "Profile updated",
            ActivityType.FailedLogin => "Failed login attempt",
            _ => activityType.ToString()
        };
    }
}
