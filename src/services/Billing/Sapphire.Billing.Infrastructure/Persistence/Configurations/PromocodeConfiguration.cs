using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the Promocode aggregate.
/// </summary>
public sealed class PromocodeConfiguration : IEntityTypeConfiguration<Promocode>
{
    public void Configure(EntityTypeBuilder<Promocode> builder)
    {
        builder.ToTable("promocodes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.NormalizedCode).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Type).IsRequired().HasConversion<string>();
        builder.Property(p => p.ValueCents).IsRequired();
        builder.Property(p => p.ValidFrom).IsRequired();
        builder.Property(p => p.ValidTo).IsRequired();
        builder.Property(p => p.MaxTotalUses).IsRequired(false);
        builder.Property(p => p.MaxUsesPerUser).IsRequired(false);
        builder.Property(p => p.IsActive).IsRequired();

        // Usages are stored in a separate table
        builder.HasMany(p => p.Usages)
            .WithOne()
            .HasForeignKey(u => u.PromocodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
