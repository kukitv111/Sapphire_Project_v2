using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Domain.Repositories;
using Sapphire.Auth.Infrastructure.Persistence;
using Sapphire.Auth.Infrastructure.Persistence.Repositories;
using Sapphire.Auth.Infrastructure.Security;
using Sapphire.Shared.Abstractions.Security;
using Sapphire.Shared.Messaging.Outbox;
using Sapphire.Shared.Security;

namespace Sapphire.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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
}
