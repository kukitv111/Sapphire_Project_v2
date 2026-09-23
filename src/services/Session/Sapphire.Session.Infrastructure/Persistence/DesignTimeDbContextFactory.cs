using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sapphire.Session.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SessionDbContext>
{
    public SessionDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=sapphire_design_time;Username=design_time";
        return new(new DbContextOptionsBuilder<SessionDbContext>().UseNpgsql(connection).Options);
    }
}
