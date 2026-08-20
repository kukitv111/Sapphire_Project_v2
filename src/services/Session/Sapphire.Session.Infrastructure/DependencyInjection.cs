using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Session.Domain.Repositories;
using Sapphire.Session.Infrastructure.Persistence;
using Sapphire.Session.Infrastructure.Persistence.Repositories;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Session.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSessionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DB Context
        services.AddDbContext<SessionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IComputerRepository, ComputerRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddSessionDatabaseInitializer(this IServiceCollection services)
    {
        services.AddScoped<SessionDatabaseInitializer>();
        return services;
    }
}

public class SessionDatabaseInitializer(SessionDbContext dbContext)
{
    public void Initialize() => dbContext.Database.EnsureCreated();
}
