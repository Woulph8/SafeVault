using System;
using System.Collections.Generic;

namespace SafeVault.Models.Generated;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime LastModifiedDate { get; set; }

    public bool IsActive { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public DateTime? LastLoginDate { get; set; }

    public int Role { get; set; }
}
