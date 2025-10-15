using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVault.Models
{
    public enum UserRole
    {
        User = 0,
        Admin = 1
    }

    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(128)]
        [Column("Salt")]
        public string Salt { get; set; } = string.Empty;

        [Required]
        [Column("Role")]
        public UserRole Role { get; set; } = UserRole.User;

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("LastModifiedDate")]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("LastLoginDate")]
        public DateTime? LastLoginDate { get; set; }
    }

    // DTO for safe data transfer
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }

        // Alias properties for view compatibility
        public DateTime CreatedAt => CreatedDate;
        public DateTime UpdatedAt => LastModifiedDate;
    }

    // Search criteria for secure querying
    public class UserSearchCriteria
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}