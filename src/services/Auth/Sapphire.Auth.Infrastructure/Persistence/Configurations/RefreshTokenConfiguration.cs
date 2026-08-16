using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the RefreshToken entity.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(256).IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at");
        builder.Property(t => t.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(200);
        builder.Property(t => t.DeviceInfo).HasColumnName("device_info").HasMaxLength(300);
        builder.Property(t => t.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(t => t.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });
        builder.HasIndex(t => t.ExpiresAt);
    }
}
