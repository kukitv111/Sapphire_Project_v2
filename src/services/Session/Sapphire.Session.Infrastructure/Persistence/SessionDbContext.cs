using Microsoft.EntityFrameworkCore;
using Sapphire.Session.Domain.Aggregates;
using Sapphire.Shared.Messaging.Outbox;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Session.Infrastructure.Persistence;

public sealed class SessionDbContext : DbContext
{
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<SessionAggregate> Sessions => Set<SessionAggregate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public SessionDbContext(DbContextOptions<SessionDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Computer>().Ignore(c => c.DomainEvents);
        modelBuilder.Entity<Computer>().Property(c => c.Status).IsConcurrencyToken();
        modelBuilder.Entity<SessionAggregate>().Ignore(s => s.DomainEvents);
        modelBuilder.Entity<SessionAggregate>().HasIndex(s => s.ComputerId)
            .IsUnique().HasFilter("\"Status\" = 0");
        // Configure SessionTimeSlot ownership
        modelBuilder.Entity<SessionAggregate>(entity =>
        {
            entity.OwnsOne(s => s.TimeSlot, slot =>
            {
                slot.Property(t => t.Start);
                slot.Property(t => t.End);
            });
        });

        base.OnModelCreating(modelBuilder);
    }
}
