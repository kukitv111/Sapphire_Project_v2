using Sapphire.Auth.Application.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Auth.Infrastructure.Persistence.Repositories;
using Sapphire.Auth.Infrastructure.Security;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DB Context
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IActivityHistoryRepository, ActivityHistoryRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddAuthDatabaseInitializer(this IServiceCollection services)
    {
        // NOTE: Temporary technical debt. Remove before production release.
        services.AddScoped<AuthDatabaseInitializer>();
        return services;
    }
}

public class AuthDatabaseInitializer(AuthDbContext dbContext)
{
    public void Initialize() => dbContext.Database.EnsureCreated();
}
