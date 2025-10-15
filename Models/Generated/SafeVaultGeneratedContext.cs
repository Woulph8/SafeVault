using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SafeVault.Models.Generated;

public partial class SafeVaultGeneratedContext : DbContext
{
    public SafeVaultGeneratedContext(DbContextOptions<SafeVaultGeneratedContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.CreatedDate, "IX_Users_CreatedDate");

            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "IX_Users_Username").IsUnique();

            entity.HasIndex(e => new { e.Username, e.Email }, "IX_Users_Username_Email");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastModifiedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .HasDefaultValue("");
            entity.Property(e => e.Salt)
                .HasMaxLength(128)
                .HasDefaultValue("");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
