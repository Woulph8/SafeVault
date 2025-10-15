using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Models;
using SafeVault.Services;
using SafeVault.Repositories;
using SafeVault.Data;

namespace SafeVault.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IInputSanitizationService _sanitizationService;
    private readonly IUserRepository _userRepository;

    public HomeController(ILogger<HomeController> logger, IInputSanitizationService sanitizationService, IUserRepository userRepository)
    {
        _logger = logger;
        _sanitizationService = sanitizationService;
        _userRepository = userRepository;
    }



    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                return RedirectToAction("Dashboard", "User");
            }
        }
        
        return RedirectToAction("Login", "Auth");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(UserInputModel model)
    {
        try
        {
            // First, validate the model using data annotations
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Sanitize and validate input using our custom service
            var sanitizedInput = _sanitizationService.SanitizeAndValidateUserInput(model);

            if (!sanitizedInput.IsValid)
            {
                // Add sanitization errors to ModelState
                foreach (var error in sanitizedInput.ValidationErrors)
                {
                    ModelState.AddModelError("", error);
                }
                return View("Index", model);
            }

            // Check if user already exists using secure parameterized query
            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(sanitizedInput.Username);
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(sanitizedInput.Email);

            if (existingUserByUsername != null)
            {
                ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                return View("Index", model);
            }

            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("Email", "Email already exists. Please use a different email address.");
                return View("Index", model);
            }

            // Create new user with sanitized data using secure parameterized query
            var newUser = new User
            {
                Username = sanitizedInput.Username,
                Email = sanitizedInput.Email,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            };

            var userId = await _userRepository.CreateUserAsync(newUser);

            // Log successful user creation with sanitized data
            _logger.LogInformation("Successfully created user: ID={UserId}, Username={Username}, Email={Email}",
                userId, sanitizedInput.Username, sanitizedInput.Email);

            // Pass sanitized data and success info to view
            ViewBag.Message = "User created successfully in the database with sanitized data!";
            ViewBag.SanitizedUsername = sanitizedInput.Username;
            ViewBag.SanitizedEmail = sanitizedInput.Email;
            ViewBag.UserId = userId;
            ViewBag.CreatedDate = newUser.CreatedDate;

            return View("Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing form submission");
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            return View("Index", model);
        }
    }

    /// <summary>
    /// Secure endpoint to search users using parameterized queries
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchUsers(string? username, string? email, bool? isActive = true)
    {
        try
        {
            var searchCriteria = new UserSearchCriteria
            {
                Username = username?.Trim(),
                Email = email?.Trim(),
                IsActive = isActive,
                PageNumber = 1,
                PageSize = 10
            };

            // Execute secure search with parameterized queries
            var users = await _userRepository.SearchUsersAsync(searchCriteria);
            
            _logger.LogInformation("User search completed. Found {Count} users", users.Count());
            
            return Json(new { 
                Success = true, 
                Users = users,
                Message = "Search completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return Json(new { 
                Success = false, 
                Message = "An error occurred while searching users" 
            });
        }
    }

    /// <summary>
    /// Secure endpoint to get user by ID using parameterized query
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Json(new { Success = false, Message = "Invalid user ID" });
            }

            // Execute secure query with parameterized ID
            var user = await _userRepository.GetUserByIdAsync(id);
            
            if (user == null)
            {
                return Json(new { Success = false, Message = "User not found" });
            }

            _logger.LogInformation("Retrieved user: {UserId}", id);
            
            return Json(new { 
                Success = true, 
                User = user,
                Message = "User retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user: {UserId}", id);
            return Json(new { 
                Success = false, 
                Message = "An error occurred while retrieving user information" 
            });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
