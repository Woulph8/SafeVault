using System.Security.Cryptography;
using System.Text;
using SafeVault.Models;
using SafeVault.Repositories;
using System.Security.Claims;

namespace SafeVault.Services
{
    public interface IUserAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(LoginViewModel model);
        Task<AuthenticationResult> RegisterAsync(RegisterViewModel model);
        Task<bool> LogoutAsync();
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model);
        string HashPassword(string password, string salt);
        string GenerateSalt();
        bool VerifyPassword(string password, string hash, string salt);
        List<Claim> CreateUserClaims(UserDto user);
    }

    public class AuthenticationService : IUserAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IInputSanitizationService _sanitizationService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserRepository userRepository, 
            IInputSanitizationService sanitizationService,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _sanitizationService = sanitizationService;
            _logger = logger;
        }

        public async Task<AuthenticationResult> LoginAsync(LoginViewModel model)
        {
            var result = new AuthenticationResult();

            try
            {
                // Sanitize input
                var sanitizedUsername = _sanitizationService.SanitizeString(model.Username);
                
                if (_sanitizationService.ContainsMaliciousContent(sanitizedUsername))
                {
                    result.Errors.Add("Username contains invalid characters");
                    return result;
                }

                // Get user from database using secure parameterized query
                var user = await _userRepository.GetUserByUsernameAsync(sanitizedUsername);
                
                if (user == null || !user.IsActive)
                {
                    result.Errors.Add("Invalid username or password");
                    _logger.LogWarning("Login attempt with invalid username: {Username}", sanitizedUsername);
                    return result;
                }

                // Get full user details for password verification
                var fullUser = await _userRepository.GetUserWithPasswordByIdAsync(user.Id);
                if (fullUser == null)
                {
                    result.Errors.Add("Invalid username or password");
                    return result;
                }

                // Verify password
                if (!VerifyPassword(model.Password, fullUser.PasswordHash, fullUser.Salt))
                {
                    result.Errors.Add("Invalid username or password");
                    _logger.LogWarning("Failed login attempt for user: {Username}", sanitizedUsername);
                    return result;
                }

                // Update last login date
                await _userRepository.UpdateLastLoginAsync(user.Id);

                result.Success = true;
                result.User = user;
                result.Message = "Login successful";

                _logger.LogInformation("Successful login for user: {Username} (ID: {UserId})", user.Username, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", model.Username);
                result.Errors.Add("An error occurred during login. Please try again.");
            }

            return result;
        }

        public async Task<AuthenticationResult> RegisterAsync(RegisterViewModel model)
        {
            var result = new AuthenticationResult();

            try
            {
                // Create UserInputModel for sanitization
                var inputModel = new UserInputModel
                {
                    Username = model.Username,
                    Email = model.Email
                };

                // Sanitize and validate input
                var sanitizedInput = _sanitizationService.SanitizeAndValidateUserInput(inputModel);
                
                if (!sanitizedInput.IsValid)
                {
                    result.Errors.AddRange(sanitizedInput.ValidationErrors);
                    return result;
                }

                // Check if user already exists
                var existingUser = await _userRepository.UserExistsAsync(sanitizedInput.Username, sanitizedInput.Email);
                if (existingUser)
                {
                    result.Errors.Add("Username or email already exists");
                    return result;
                }

                // Generate salt and hash password
                var salt = GenerateSalt();
                var passwordHash = HashPassword(model.Password, salt);

                // Create new user
                var newUser = new User
                {
                    Username = sanitizedInput.Username,
                    Email = sanitizedInput.Email,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    Role = model.Role,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    IsActive = true
                };

                var userId = await _userRepository.CreateUserAsync(newUser);

                // Create DTO for result
                var userDto = new UserDto
                {
                    Id = userId,
                    Username = newUser.Username,
                    Email = newUser.Email,
                    Role = newUser.Role,
                    CreatedDate = newUser.CreatedDate,
                    IsActive = newUser.IsActive
                };

                result.Success = true;
                result.User = userDto;
                result.Message = "User registered successfully";

                _logger.LogInformation("New user registered: {Username} (ID: {UserId}) with role: {Role}", 
                    newUser.Username, userId, newUser.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for username: {Username}", model.Username);
                result.Errors.Add("An error occurred during registration. Please try again.");
            }

            return result;
        }

        public async Task<bool> LogoutAsync()
        {
            // In a real application, you might want to invalidate tokens or update last activity
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model)
        {
            try
            {
                var user = await _userRepository.GetUserWithPasswordByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Verify current password
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash, user.Salt))
                {
                    return false;
                }

                // Generate new salt and hash new password
                var newSalt = GenerateSalt();
                var newPasswordHash = HashPassword(model.NewPassword, newSalt);

                // Update password in database
                return await _userRepository.UpdatePasswordAsync(userId, newPasswordHash, newSalt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + salt;
            var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hashBytes = sha256.ComputeHash(saltedPasswordBytes);
            return Convert.ToBase64String(hashBytes);
        }

        public string GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            var computedHash = HashPassword(password, salt);
            return computedHash == hash;
        }

        public List<Claim> CreateUserClaims(UserDto user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("UserId", user.Id.ToString()),
                new("Username", user.Username)
            };

            return claims;
        }
    }
}