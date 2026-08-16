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
        // Configure SessionTimeSlot ownership
        modelBuilder.Entity<SessionAggregate>(entity =>
        {
            entity.OwnsOne(s => s.TimeSlot);
        });

        base.OnModelCreating(modelBuilder);
    }
}
