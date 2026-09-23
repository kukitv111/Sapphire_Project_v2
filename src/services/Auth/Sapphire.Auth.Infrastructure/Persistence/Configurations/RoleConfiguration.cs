using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Role entity.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(r => r.NormalizedName).HasColumnName("normalized_name").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(r => r.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(r => r.DomainEvents);
        builder.Ignore(r => r.UserRoles);

        builder.HasMany(r => r.Permissions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "role_permissions",
                right => right.HasOne<Permission>()
                    .WithMany()
                    .HasForeignKey("permission_id")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Role>()
                    .WithMany()
                    .HasForeignKey("role_id")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("role_id", "permission_id");
                    join.ToTable("role_permissions");
                    join.Property<DateTime>("assigned_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                });
        builder.Navigation(r => r.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.Name).IsUnique();
        builder.HasIndex(r => r.NormalizedName).IsUnique();
    }
}
