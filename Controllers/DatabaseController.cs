using Microsoft.AspNetCore.Mvc;
using SafeVault.Repositories;
using SafeVault.Models;
using SafeVault.Data;
using SafeVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace SafeVault.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DatabaseController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly SafeVaultDbContext _context;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(IUserRepository userRepository, SafeVaultDbContext context, ILogger<DatabaseController> logger)
        {
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Viewer()
        {
            try
            {
                var searchCriteria = new UserSearchCriteria { PageSize = 1000 };
                var users = await _userRepository.SearchUsersAsync(searchCriteria);
                
                ViewBag.TotalUsers = users.Count();
                ViewBag.ActiveUsers = users.Count(u => u.IsActive);
                ViewBag.AdminUsers = users.Count(u => u.Role == UserRole.Admin);
                ViewBag.RegularUsers = users.Count(u => u.Role == UserRole.User);
                ViewBag.UsersWithLogin = users.Count(u => u.LastLoginDate.HasValue);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing database");
                TempData["ErrorMessage"] = "Error loading database information.";
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PasswordHashInfo()
        {
            try
            {
                // Fix: Use dependency injection instead of hardcoded connection string
                // Get users with their actual password hashes for security verification

                var usersWithHashes = await _context.Users
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role,
                        PasswordHashLength = u.PasswordHash.Length,
                        PasswordHashPrefix = u.PasswordHash.Substring(0, Math.Min(10, u.PasswordHash.Length)),
                        SaltLength = u.Salt.Length,
                        SaltPrefix = u.Salt.Substring(0, Math.Min(8, u.Salt.Length)),
                        u.CreatedDate,
                        HasValidHash = u.PasswordHash.Length > 50, // SHA256 hashes should be longer
                        HasValidSalt = u.Salt.Length > 10
                    })
                    .ToListAsync();

                ViewBag.SecurityAnalysis = new {
                    TotalUsers = usersWithHashes.Count,
                    UsersWithValidHashes = usersWithHashes.Count(u => u.HasValidHash),
                    UsersWithValidSalts = usersWithHashes.Count(u => u.HasValidSalt),
                    AverageHashLength = usersWithHashes.Average(u => u.PasswordHashLength),
                    AverageSaltLength = usersWithHashes.Average(u => u.SaltLength)
                };

                return View(usersWithHashes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing password hash information");
                TempData["ErrorMessage"] = "Error loading password security information.";
                return RedirectToAction("Viewer");
            }
        }

        [HttpGet]
        public IActionResult TestPasswordHashing(string testPassword = "TestPassword123!")
        {
            try
            {
                // For demonstration purposes - showing how password hashing works
                var authService = HttpContext.RequestServices.GetRequiredService<IUserAuthenticationService>();
                
                // Generate salt and hash
                var salt = authService.GenerateSalt();
                var hash = authService.HashPassword(testPassword, salt);
                
                // Verify the hash
                var isValid = authService.VerifyPassword(testPassword, hash, salt);
                var isInvalidWithWrongPassword = authService.VerifyPassword("WrongPassword", hash, salt);
                
                var result = new
                {
                    TestPassword = testPassword,
                    GeneratedSalt = salt,
                    GeneratedHash = hash,
                    HashLength = hash.Length,
                    SaltLength = salt.Length,
                    VerificationWithCorrectPassword = isValid,
                    VerificationWithWrongPassword = isInvalidWithWrongPassword,
                    HashingMethod = "SHA-256 with Salt",
                    SecurityStatus = isValid && !isInvalidWithWrongPassword ? "✅ Working Correctly" : "❌ Error in hashing"
                };
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing password hashing");
                return Json(new { Error = "Password hashing test failed", Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportData()
        {
            try
            {
                var searchCriteria = new UserSearchCriteria { PageSize = 1000 };
                var users = await _userRepository.SearchUsersAsync(searchCriteria);
                
                var csv = "ID,Username,Email,Role,IsActive,CreatedDate,LastModifiedDate,LastLoginDate\n";
                
                foreach (var user in users)
                {
                    csv += $"{user.Id},{user.Username},{user.Email},{user.Role},{user.IsActive}," +
                           $"{user.CreatedDate:yyyy-MM-dd HH:mm:ss},{user.LastModifiedDate:yyyy-MM-dd HH:mm:ss}," +
                           $"{(user.LastLoginDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never")}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"SafeVault_Users_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                TempData["ErrorMessage"] = "Error exporting data.";
                return RedirectToAction("Viewer");
            }
        }
    }
}