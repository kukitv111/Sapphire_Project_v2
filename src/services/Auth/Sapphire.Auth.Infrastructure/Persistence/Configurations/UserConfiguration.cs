using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Infrastructure.Persistence.Converters;

namespace Sapphire.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the User aggregate.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasConversion(AuthValueConverters.UsernameConverter())
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasConversion(AuthValueConverters.EmailConverter())
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasConversion(AuthValueConverters.PhoneConverter())
            .HasMaxLength(20);

        // Password as owned value object (hash + salt columns)
        builder.OwnsOne(u => u.Password, p =>
        {
            p.Property(x => x.Hash).HasColumnName("password_hash").IsRequired();
            p.Property(x => x.Salt).HasColumnName("password_salt").IsRequired();
        });

        builder.Property(u => u.BranchId).HasColumnName("branch_id");
        builder.Property(u => u.BonusBalanceCents).HasColumnName("bonus_balance_cents").HasDefaultValue(0L);

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.BanReason).HasColumnName("ban_reason").HasMaxLength(500);
        builder.Property(u => u.BannedAt).HasColumnName("banned_at");
        builder.Property(u => u.BannedBy).HasColumnName("banned_by");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.LastLoginIp).HasColumnName("last_login_ip").HasMaxLength(45);
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until");

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");

        // Roles as owned collection (table user_roles)
        builder.OwnsMany(u => u.Roles, r =>
        {
            r.ToTable("user_roles");
            r.WithOwner().HasForeignKey("user_id");
            r.Property(x => x.RoleId).HasColumnName("role_id");
            r.Property(x => x.AssignedBy).HasColumnName("assigned_by");
            r.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            r.HasKey("user_id", "role_id");
        });
        builder.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Refresh tokens as separate entity (one-to-many)
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Domain events are transient, not persisted directly (outbox handles them)
        builder.Ignore(u => u.DomainEvents);

        // Unique indexes
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Phone).IsUnique();
        builder.HasIndex(u => u.BranchId);
    }
}
