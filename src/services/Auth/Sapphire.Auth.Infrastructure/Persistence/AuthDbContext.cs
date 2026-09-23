using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityHistory> ActivityHistories => Set<ActivityHistory>();
    public DbSet<ActivityHistory> ActivityHistory => Set<ActivityHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
