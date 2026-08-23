namespace Sapphire.Shared.Kernel.Security;

/// <summary>
/// Canonical permission codes used across all Sapphire services.
/// Permission codes are stored upper-cased (see the Permission entity) and are
/// emitted verbatim into the "permission" claim of every access token.
/// Role-based policies are the primary authorization mechanism; permission
/// claims provide the finer-grained secondary layer.
/// </summary>
public static class SapphirePermissions
{
    // Users & roles
    public const string UsersRead = "USERS.READ";
    public const string RolesManage = "ROLES.MANAGE";

    // Billing
    public const string TariffsManage = "BILLING.TARIFFS.MANAGE";
    public const string TariffsAssign = "BILLING.TARIFFS.ASSIGN";
    public const string SettingsManage = "BILLING.SETTINGS.MANAGE";
    public const string WalletsTopUp = "BILLING.WALLETS.TOPUP";
    public const string AnyWalletAccess = "BILLING.WALLETS.ACCESS_ANY";

    // Sessions
    public const string AnySessionRead = "SESSIONS.READ_ANY";
    public const string AnySessionManage = "SESSIONS.MANAGE_ANY";
}
