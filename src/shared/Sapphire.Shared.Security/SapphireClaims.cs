namespace Sapphire.Shared.Security;

/// <summary>
/// Canonical JWT claim types for the Sapphire platform.
///
/// The canonical role claim format is the short "role" claim type carrying the
/// verbatim role name (see <see cref="Sapphire.Shared.Kernel.Security.SapphireRoles"/>),
/// and the "permission" claim type carrying the permission code. Token validation
/// maps the role claim via <c>TokenValidationParameters.RoleClaimType</c>, so both
/// <c>[Authorize(Roles = ...)]</c> and <c>RequireRole</c> policies work with the
/// short claim. Legacy <c>ClaimTypes.Role</c> (the long WS-* URI) is NOT emitted
/// and NOT read anywhere.
/// </summary>
public static class SapphireClaims
{
    /// <summary>Subject — authenticated user id (GUID string).</summary>
    public const string UserId = "sub";

    /// <summary>User email address.</summary>
    public const string Email = "email";

    /// <summary>Access token unique id.</summary>
    public const string Jti = "jti";

    /// <summary>Canonical role claim type: carries a role name.</summary>
    public const string Role = "role";

    /// <summary>Canonical permission claim type: carries a permission code.</summary>
    public const string Permission = "permission";
}
