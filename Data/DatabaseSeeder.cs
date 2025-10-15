using SafeVault.Models;
using SafeVault.Data;
using SafeVault.Services;

namespace SafeVault.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(SafeVaultDbContext context, IUserAuthenticationService authService)
        {
            // Check if admin user already exists
            var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
            
            if (existingAdmin != null && (string.IsNullOrEmpty(existingAdmin.PasswordHash) || string.IsNullOrEmpty(existingAdmin.Salt)))
            {
                // Update existing admin user with proper password
                var salt = authService.GenerateSalt();
                var passwordHash = authService.HashPassword("Admin123!", salt);
                
                existingAdmin.PasswordHash = passwordHash;
                existingAdmin.Salt = salt;
                existingAdmin.Role = UserRole.Admin;
                existingAdmin.Email = "admin@safevault.com";
                existingAdmin.LastModifiedDate = DateTime.UtcNow;
                existingAdmin.IsActive = true;

                context.Users.Update(existingAdmin);
                await context.SaveChangesAsync();
                
                Console.WriteLine("✅ Admin user updated with credentials:");
                Console.WriteLine("   Username: admin");
                Console.WriteLine("   Password: Admin123!");
                Console.WriteLine("   Email: admin@safevault.com");
            }
            else if (existingAdmin == null)
            {
                // Create initial admin user
                var salt = authService.GenerateSalt();
                var passwordHash = authService.HashPassword("Admin123!", salt);

                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@safevault.com",
                    PasswordHash = passwordHash,
                    Salt = salt,
                    Role = UserRole.Admin,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    IsActive = true
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                
                Console.WriteLine("✅ Initial admin user created:");
                Console.WriteLine("   Username: admin");
                Console.WriteLine("   Password: Admin123!");
                Console.WriteLine("   Email: admin@safevault.com");
            }

            // Create a sample regular user for testing
            var existingUser = context.Users.FirstOrDefault(u => u.Username == "testuser");
            
            if (existingUser == null)
            {
                var salt = authService.GenerateSalt();
                var passwordHash = authService.HashPassword("User123!", salt);

                var regularUser = new User
                {
                    Username = "testuser",
                    Email = "testuser@safevault.com",
                    PasswordHash = passwordHash,
                    Salt = salt,
                    Role = UserRole.User,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    IsActive = true
                };

                context.Users.Add(regularUser);
                await context.SaveChangesAsync();
                
                Console.WriteLine("✅ Test user created:");
                Console.WriteLine("   Username: testuser");
                Console.WriteLine("   Password: User123!");
                Console.WriteLine("   Email: testuser@safevault.com");
            }
        }
    }
}