using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Shared.Kernel.Security;
using System.Threading.RateLimiting;

namespace Sapphire.Shared.Security;

/// <summary>
/// Centralized authorization policy and rate-limiting registration.
/// All three services call <c>AddSapphireAuthorization()</c> once in Program.cs.
/// </summary>
public static class SapphireAuthorizationExtensions
{
    public static IServiceCollection AddSapphireAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.OwnerOnly,
                p => p.RequireRole(SapphireRoles.Owner));

            options.AddPolicy(PolicyNames.AdminOnly,
                p => p.RequireRole(SapphireRoles.Admin, SapphireRoles.Owner));

            options.AddPolicy(PolicyNames.CashierOrAdmin,
                p => p.RequireRole(SapphireRoles.Cashier, SapphireRoles.Admin, SapphireRoles.Owner));

            options.AddPolicy(PolicyNames.AuthenticatedUser,
                p => p.RequireAuthenticatedUser());
        });

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(PolicyNames.AuthRateLimit, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    public static WebApplication UseSapphireMiddleware(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }
}

/// <summary>
/// Canonical policy names referenced by <c>[Authorize(Policy = ...)]</c>.
/// </summary>
public static class PolicyNames
{
    public const string OwnerOnly = "OwnerOnly";
    public const string AdminOnly = "AdminOnly";
    public const string CashierOrAdmin = "CashierOrAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string AuthRateLimit = "AuthRateLimit";
}
