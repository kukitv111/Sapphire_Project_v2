using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sapphire.Shared.Security;

public static class ProductionConfiguration
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Database connection configuration is required");
        var parsed = new DbConnectionStringBuilder { ConnectionString = connection };
        if (!parsed.TryGetValue("Password", out var password) || string.IsNullOrWhiteSpace(password.ToString())
            || password.ToString()!.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || password.ToString() == "admin"
            || password.ToString() == "your_secure_db_password")
            throw new InvalidOperationException("A non-default database password is required");
    }
}
