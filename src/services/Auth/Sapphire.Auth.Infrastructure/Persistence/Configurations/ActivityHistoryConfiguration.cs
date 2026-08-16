using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ActivityHistory entity (audit log).
/// </summary>
public sealed class ActivityHistoryConfiguration : IEntityTypeConfiguration<ActivityHistory>
{
    public void Configure(EntityTypeBuilder<ActivityHistory> builder)
    {
        builder.ToTable("activity_history");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.ActivityType).HasColumnName("activity_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(50);
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(a => a.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.DeviceInfo).HasColumnName("device_info").HasMaxLength(300);
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(a => new { a.UserId, a.OccurredAt });
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.ActivityType);
    }
}
