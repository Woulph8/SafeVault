using Microsoft.EntityFrameworkCore;
using SafeVault.Data;
using SafeVault.Models;

namespace SafeVault.Tools
{
    public class DatabaseViewer
    {
        public static async Task ShowAllUsers()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SafeVaultDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
            
            var options = new DbContextOptionsBuilder<SafeVaultDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var context = new SafeVaultDbContext(options);
            
            Console.WriteLine("=== SafeVault Database - Users Table ===");
            Console.WriteLine();
            
            var users = await context.Users.ToListAsync();
            
            Console.WriteLine($"Total Users: {users.Count}");
            Console.WriteLine();
            Console.WriteLine("| ID | Username     | Email                    | Role  | Active | Created Date        | Last Login          |");
            Console.WriteLine("|----+-------------+--------------------------+-------+--------+---------------------+---------------------|");
            
            foreach (var user in users)
            {
                var lastLogin = user.LastLoginDate?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
                var createdDate = user.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                var isActive = user.IsActive ? "Yes" : "No";
                
                Console.WriteLine($"| {user.Id,2} | {user.Username,-11} | {user.Email,-24} | {user.Role,-5} | {isActive,-6} | {createdDate,-19} | {lastLogin,-19} |");
            }
            
            Console.WriteLine();
            Console.WriteLine("=== Database Statistics ===");
            Console.WriteLine($"Active Users: {users.Count(u => u.IsActive)}");
            Console.WriteLine($"Admin Users: {users.Count(u => u.Role == UserRole.Admin)}");
            Console.WriteLine($"Regular Users: {users.Count(u => u.Role == UserRole.User)}");
            Console.WriteLine($"Users with Recent Login: {users.Count(u => u.LastLoginDate.HasValue)}");
        }
    }
}