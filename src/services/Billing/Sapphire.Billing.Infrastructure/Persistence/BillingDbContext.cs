using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
    public DbSet<TariffEntitlement> Entitlements => Set<TariffEntitlement>();
    public DbSet<SessionReservation> SessionReservations => Set<SessionReservation>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Promocode> Promocodes => Set<Promocode>();
    public DbSet<PromocodeUsage> PromocodeUsages => Set<PromocodeUsage>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        modelBuilder.Entity<TariffEntitlement>(entity =>
        {
            entity.ToTable("tariff_entitlements");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.DomainEvents);
            entity.HasIndex(e => new { e.UserId, e.IdempotencyKey }).IsUnique();
            entity.Property(e => e.AvailableCents).IsConcurrencyToken();
            entity.Property(e => e.ReservedCents).IsConcurrencyToken();
        });
        modelBuilder.Entity<SessionReservation>(entity =>
        {
            entity.ToTable("session_reservations");
            entity.HasKey(r => r.Id);
            entity.Ignore(r => r.DomainEvents);
            entity.HasIndex(r => r.SessionId).IsUnique();
        });
        modelBuilder.Entity<Wallet>().Ignore(w => w.DomainEvents);
        modelBuilder.Entity<Tariff>().Ignore(t => t.DomainEvents);
        modelBuilder.Entity<Promocode>().Ignore(p => p.DomainEvents);
        // Usage IDs are assigned by the domain. EF must insert newly attached usages
        // discovered through a loaded promocode's collection, not update them.
        modelBuilder.Entity<PromocodeUsage>().Property(u => u.Id).ValueGeneratedNever();
        modelBuilder.Entity<Wallet>().HasIndex(w => w.UserId).IsUnique();
        modelBuilder.Entity<Promocode>().HasIndex(p => p.NormalizedCode).IsUnique();
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(m => m.EventId);
            entity.Property(m => m.Type).HasMaxLength(160);
        });
        base.OnModelCreating(modelBuilder);
    }
}
