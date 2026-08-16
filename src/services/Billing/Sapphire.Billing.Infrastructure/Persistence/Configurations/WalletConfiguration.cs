using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the Wallet aggregate.
/// </summary>
public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).IsRequired();

        builder.OwnsOne(w => w.MainBalance, money =>
        {
            money.Property(m => m.Cents).HasColumnName("main_balance_cents").IsRequired().HasDefaultValue(0);
        });

        builder.OwnsOne(w => w.BonusBalance, money =>
        {
            money.Property(m => m.Cents).HasColumnName("bonus_balance_cents").IsRequired().HasDefaultValue(0);
        });
    }
}
