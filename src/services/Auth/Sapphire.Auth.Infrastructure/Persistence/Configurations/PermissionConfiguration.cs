using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Permission entity.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(50);
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
