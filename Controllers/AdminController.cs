using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Models;
using SafeVault.Services;
using SafeVault.Repositories;

namespace SafeVault.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserAuthenticationService authService, 
            IUserRepository userRepository,
            ILogger<AdminController> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                _logger.LogInformation("Loading admin dashboard for user: {Username}", User.Identity?.Name);

                // Get comprehensive user statistics
                var allUsersCriteria = new UserSearchCriteria { PageSize = 1000 }; // Get all users
                var allUsers = await _userRepository.SearchUsersAsync(allUsersCriteria);

                // Calculate statistics
                var totalUsers = allUsers.Count();
                var adminCount = allUsers.Count(u => u.Role == UserRole.Admin);
                var regularUserCount = allUsers.Count(u => u.Role == UserRole.User);
                var activeUsers = allUsers.Count(u => u.IsActive);
                var recentUsers = allUsers.Count(u => u.CreatedDate >= DateTime.UtcNow.AddDays(-7));
                var todaysLogins = allUsers.Count(u => u.LastLoginDate?.Date == DateTime.UtcNow.Date);

                // Get recent user activity
                var recentUsersList = allUsers
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(5)
                    .ToList();

                // Pass data to view
                ViewBag.TotalUsers = totalUsers;
                ViewBag.AdminCount = adminCount;
                ViewBag.RegularUserCount = regularUserCount;
                ViewBag.ActiveUsers = activeUsers;
                ViewBag.RecentUsers = recentUsers;
                ViewBag.TodaysLogins = todaysLogins;
                ViewBag.RecentUsersList = recentUsersList;
                ViewBag.AdminUsername = User.Identity?.Name ?? "Admin";

                _logger.LogInformation("Dashboard loaded successfully - Total Users: {TotalUsers}, Admins: {AdminCount}, Regular Users: {RegularUserCount}", 
                    totalUsers, adminCount, regularUserCount);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                // Return view with default values in case of error
                ViewBag.TotalUsers = 0;
                ViewBag.AdminCount = 0;
                ViewBag.RegularUserCount = 0;
                ViewBag.ActiveUsers = 0;
                ViewBag.RecentUsers = 0;
                ViewBag.TodaysLogins = 0;
                ViewBag.RecentUsersList = new List<UserDto>();
                ViewBag.AdminUsername = User.Identity?.Name ?? "Admin";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _authService.RegisterAsync(model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }

                TempData["SuccessMessage"] = $"User '{result.User?.Username}' with role '{model.Role}' has been created successfully!";
                _logger.LogInformation("Admin {AdminUser} created new user: {NewUser} with role: {Role}", 
                    User.Identity?.Name, result.User?.Username, model.Role);

                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration by admin");
                ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserManagement(string? search, UserRole? role, string? sortBy)
        {
            try
            {
                var searchCriteria = new UserSearchCriteria
                {
                    Username = search?.Trim(),
                    Email = search?.Trim(),
                    IsActive = true,
                    PageNumber = 1,
                    PageSize = 100
                };

                var users = await _userRepository.SearchUsersAsync(searchCriteria);

                // Filter by role if specified
                if (role.HasValue)
                {
                    users = users.Where(u => u.Role == role.Value).ToList();
                }

                // Apply sorting
                users = sortBy?.ToLower() switch
                {
                    "email" => users.OrderBy(u => u.Email).ToList(),
                    "role" => users.OrderBy(u => u.Role).ToList(),
                    "created" => users.OrderByDescending(u => u.CreatedDate).ToList(),
                    _ => users.OrderBy(u => u.Username).ToList()
                };

                // Pass filter values to view
                ViewBag.SearchTerm = search;
                ViewBag.RoleFilter = role;
                ViewBag.SortBy = sortBy ?? "username";

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management page");
                return View(new List<UserDto>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                
                if (userId == currentUserId)
                {
                    TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                    return RedirectToAction("UserManagement");
                }

                var result = await _userRepository.DeactivateUserAsync(userId);
                
                if (result)
                {
                    TempData["SuccessMessage"] = "User has been deactivated successfully.";
                    _logger.LogInformation("Admin {AdminUser} deactivated user ID: {UserId}", 
                        User.Identity?.Name, userId);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to deactivate user.";
                }

                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while deactivating the user.";
                return RedirectToAction("UserManagement");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserManagement");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details: {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToAction("UserManagement");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                // Get comprehensive user statistics
                var allUsersCriteria = new UserSearchCriteria { PageSize = 1000 }; // Get all users
                var allUsers = await _userRepository.SearchUsersAsync(allUsersCriteria);

                // Calculate statistics
                var stats = new
                {
                    TotalUsers = allUsers.Count(),
                    AdminUsers = allUsers.Count(u => u.Role == UserRole.Admin),
                    RegularUsers = allUsers.Count(u => u.Role == UserRole.User),
                    ActiveUsers = allUsers.Count(u => u.IsActive),
                    RecentUsers = allUsers.Count(u => u.CreatedDate >= DateTime.UtcNow.AddDays(-7)),
                    TodaysLogins = allUsers.Count(u => u.LastLoginDate?.Date == DateTime.UtcNow.Date),
                    Error = false
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return Json(new { Error = true, Message = "Error loading statistics" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SystemLogs()
        {
            // In a real application, you would load system logs from a logging service
            // For now, this is just a placeholder
            await Task.CompletedTask;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SecuritySettings()
        {
            // Placeholder for security configuration settings
            await Task.CompletedTask;
            return View();
        }


    }
}