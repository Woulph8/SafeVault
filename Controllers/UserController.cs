using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Repositories;

namespace SafeVault.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepository userRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var username = User.Identity?.Name ?? "User";

                _logger.LogInformation("Dashboard accessed by user: {Username}, UserIdClaim: {UserIdClaim}", username, userIdClaim);

                if (int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogInformation("Attempting to get user details for ID: {UserId}", userId);
                    var userDetails = await _userRepository.GetUserByIdAsync(userId);
                    
                    if (userDetails != null)
                    {
                        _logger.LogInformation("Successfully loaded user details for: {Username}", userDetails.Username);
                        return View(userDetails);
                    }
                    else
                    {
                        _logger.LogWarning("UserRepository returned null for ID: {UserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse UserIdClaim: {UserIdClaim}", userIdClaim);
                }

                // Fallback if user details can't be loaded
                _logger.LogWarning("Could not load user details for user: {Username}", username);
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard for user: {Username}", User.Identity?.Name);
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToAction("Dashboard");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Dashboard");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile: {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ActivityLog()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToAction("Dashboard");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                ViewBag.UserDetails = user;

                // In a real application, you would load the user's activity log
                // For now, this is just a placeholder
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity log for user: {Username}", User.Identity?.Name);
                return View();
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult TryAccessAdmin()
        {
            // This action is only accessible to Admin users
            // Regular users will be redirected to AccessDenied
            return View();
        }
    }
}