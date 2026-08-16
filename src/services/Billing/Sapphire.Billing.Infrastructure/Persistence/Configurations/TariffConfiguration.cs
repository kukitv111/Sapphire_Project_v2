using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the Tariff aggregate.
/// </summary>
public sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("tariffs");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Type).IsRequired().HasConversion<string>();
        builder.Property(t => t.PricePerMinuteCents).IsRequired();
        builder.Property(t => t.PricePerHourCents).IsRequired();
        builder.Property(t => t.PackageDurationMinutes).IsRequired(false);
        builder.Property(t => t.PackageBonusMinutes).IsRequired(false);
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.IsSystem).IsRequired();
    }
}
