using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Kernel.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityHistory> ActivityHistories => Set<ActivityHistory>();
    public DbSet<ActivityHistory> ActivityHistory => Set<ActivityHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    private readonly IConfiguration _configuration;

    public AuthDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация Value Objects (Email, Username, Password)
        modelBuilder.Entity<User>(entity =>
        {
            entity.OwnsOne(u => u.Email);
            entity.OwnsOne(u => u.Username);
            entity.OwnsOne(u => u.Password);
            entity.OwnsOne(u => u.Phone);
        });

        base.OnModelCreating(modelBuilder);
    }
}
