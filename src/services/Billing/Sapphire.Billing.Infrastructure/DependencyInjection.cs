using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Billing.Domain.Repositories;
using Sapphire.Billing.Infrastructure.Persistence;
using Sapphire.Billing.Infrastructure.Persistence.Repositories;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DB Context
        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITariffRepository, TariffRepository>();
        services.AddScoped<IPromocodeRepository, PromocodeRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddBillingDatabaseInitializer(this IServiceCollection services)
    {
        services.AddScoped<BillingDatabaseInitializer>();
        return services;
    }
}

public class BillingDatabaseInitializer(BillingDbContext dbContext)
{
    public void Initialize() => dbContext.Database.EnsureCreated();
}
