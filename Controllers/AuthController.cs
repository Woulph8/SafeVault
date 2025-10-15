using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Models;
using SafeVault.Services;
using System.Security.Claims;

namespace SafeVault.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "User");
            }

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                _logger.LogInformation("Login attempt from user: {Username}", model.Username);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for login attempt: {Username}", model.Username);
                    return View(model);
                }

                var result = await _authService.LoginAsync(model);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }

                if (result.User == null)
                {
                    ModelState.AddModelError("", "An error occurred during login");
                    return View(model);
                }

                // Create authentication claims
                var claims = _authService.CreateUserClaims(result.User);
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User logged in successfully: {Username}", result.User.Username);

                // Redirect based on role and return URL
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (result.User.Role == UserRole.Admin)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    return RedirectToAction("Dashboard", "User");
                }
            }
            catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException ex)
            {
                _logger.LogWarning(ex, "Antiforgery validation failed for user: {Username}", model.Username);
                ModelState.AddModelError("", "Security validation failed. Please refresh the page and try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for user: {Username}", model.Username);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = User.Identity?.Name ?? "Unknown";
                
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await _authService.LogoutAsync();

                _logger.LogInformation("User logged out: {Username}", username);

                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process");
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RefreshToken()
        {
            // This endpoint allows getting a fresh anti-forgery token
            // The browser will need to refresh the page to get a new token
            return Json(new { message = "Please refresh the page to get a new security token" });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // You can fetch additional user details here if needed
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                return RedirectToAction("Dashboard", "User");
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    ModelState.AddModelError("", "Unable to verify user identity");
                    return View(model);
                }

                var result = await _authService.ChangePasswordAsync(userId, model);

                if (result)
                {
                    TempData["SuccessMessage"] = "Password changed successfully";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("", "Current password is incorrect or an error occurred");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                ModelState.AddModelError("", "An error occurred while changing your password");
                return View(model);
            }
        }
    }
}