using Microsoft.EntityFrameworkCore;
using SafeVault.Models;

namespace SafeVault.Data
{
    public class SafeVaultDbContext : DbContext
    {
        public SafeVaultDbContext(DbContextOptions<SafeVaultDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.LastModifiedDate)
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Add indexes for better performance and uniqueness constraints
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Username");
                
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");
                
                entity.HasIndex(e => new { e.Username, e.Email })
                    .HasDatabaseName("IX_Users_Username_Email");
                
                entity.HasIndex(e => e.CreatedDate)
                    .HasDatabaseName("IX_Users_CreatedDate");
            });

            // Seed some test data (optional) - using static dates to avoid migration issues
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@safevault.com",
                    CreatedDate = new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                    LastModifiedDate = new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be overridden by dependency injection configuration
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SafeVaultDb;Trusted_Connection=true;MultipleActiveResultSets=true");
            }
        }
    }
}