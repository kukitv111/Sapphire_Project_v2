using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Session.Application;
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
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpClient<IBillingReservationClient, BillingReservationClient>(client =>
        {
            var url = configuration["Billing:InternalUrl"] ?? "http://localhost:5191/";
            client.BaseAddress = new Uri(url.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
