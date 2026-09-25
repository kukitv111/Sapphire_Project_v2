using Microsoft.EntityFrameworkCore;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BootstrapRecord> BootstrapRecords => Set<BootstrapRecord>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityHistory> ActivityHistories => Set<ActivityHistory>();
    public DbSet<ActivityHistory> ActivityHistory => Set<ActivityHistory>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BootstrapRecord>(entity =>
        {
            entity.ToTable("bootstrap_state");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
        });
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(m => m.EventId);
            entity.Property(m => m.Type).HasMaxLength(160);
        });
        base.OnModelCreating(modelBuilder);
    }
}
