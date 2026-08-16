using Microsoft.EntityFrameworkCore;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Billing.Domain.Entities;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Promocode> Promocodes => Set<Promocode>();
    public DbSet<PromocodeUsage> PromocodeUsages => Set<PromocodeUsage>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
