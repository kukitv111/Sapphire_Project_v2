using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Shared.Messaging.Outbox;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the outbox table (shared messaging contract).
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
        builder.Property(m => m.Content).HasColumnName("content").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredOn).HasColumnName("occurred_on").IsRequired();
        builder.Property(m => m.ProcessedOn).HasColumnName("processed_on");
        builder.Property(m => m.Error).HasColumnName("error").HasColumnType("text");
        builder.Property(m => m.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(m => new { m.ProcessedOn, m.OccurredOn });
        builder.HasIndex(m => m.OccurredOn);
    }
}
