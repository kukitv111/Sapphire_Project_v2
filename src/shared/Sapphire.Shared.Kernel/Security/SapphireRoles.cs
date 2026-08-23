namespace Sapphire.Shared.Kernel.Security;

/// <summary>
/// Canonical role names used across all Sapphire services.
/// Role names are the single source of truth for role-based authorization
/// and are emitted verbatim into the "role" claim of every access token.
/// </summary>
public static class SapphireRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string User = "User";

    /// <summary>All system roles in descending privilege order.</summary>
    public static readonly IReadOnlyList<string> All = [Owner, Admin, Cashier, User];

    /// <summary>
    /// Roles that grant elevated (staff) access: they may act on resources
    /// owned by other users where a policy explicitly allows it.
    /// </summary>
    public static readonly IReadOnlyList<string> Elevated = [Owner, Admin, Cashier];

    /// <summary>Roles allowed to administer the system (hierarchy: Admin inherits Admin-only rights for Owner too).</summary>
    public static readonly IReadOnlyList<string> Administrative = [Owner, Admin];
}
