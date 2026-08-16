using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sapphire.Auth.Infrastructure.Persistence;

namespace Sapphire.Auth.Api.HealthChecks;

/// <summary>
/// Verifies PostgreSQL connectivity by executing a trivial query against the Auth database.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AuthDbContext _dbContext;

    public DatabaseHealthCheck(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable", exception);
        }
    }
}
