using Microsoft.EntityFrameworkCore;
using SafeVault.Data;
using SafeVault.Models;
using System.Data;

namespace SafeVault.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SafeVaultDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(SafeVaultDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Securely retrieve user by ID using parameterized query
        /// </summary>
        /// <param name="id">User ID parameter</param>
        /// <returns>UserDto or null if not found</returns>
        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            try
            {
                // Using Entity Framework's parameterized query (SQL injection safe)
                var user = await _context.Users
                    .Where(u => u.Id == id && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedDate = u.CreatedDate,
                        LastModifiedDate = u.LastModifiedDate,
                        LastLoginDate = u.LastLoginDate,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Retrieved user by ID: {UserId}", id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Securely retrieve user by username using parameterized query
        /// </summary>
        /// <param name="username">Username parameter (will be sanitized)</param>
        /// <returns>UserDto or null if not found</returns>
        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                // Parameterized query - EF Core automatically handles SQL injection prevention
                var user = await _context.Users
                    .Where(u => u.Username == username && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedDate = u.CreatedDate,
                        LastModifiedDate = u.LastModifiedDate,
                        LastLoginDate = u.LastLoginDate,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Retrieved user by username: {Username}", username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Securely retrieve user by email using parameterized query
        /// </summary>
        /// <param name="email">Email parameter (will be sanitized)</param>
        /// <returns>UserDto or null if not found</returns>
        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                // Parameterized query with case-insensitive email search
                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedDate = u.CreatedDate,
                        LastModifiedDate = u.LastModifiedDate,
                        LastLoginDate = u.LastLoginDate,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Retrieved user by email: {Email}", email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Securely search users with multiple criteria using parameterized queries
        /// </summary>
        /// <param name="criteria">Search criteria with built-in pagination</param>
        /// <returns>Collection of matching UserDto objects</returns>
        public async Task<IEnumerable<UserDto>> SearchUsersAsync(UserSearchCriteria criteria)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Apply filters using parameterized queries
                if (!string.IsNullOrWhiteSpace(criteria.Username))
                {
                    query = query.Where(u => u.Username.Contains(criteria.Username));
                }

                if (!string.IsNullOrWhiteSpace(criteria.Email))
                {
                    query = query.Where(u => u.Email.Contains(criteria.Email));
                }

                if (criteria.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == criteria.IsActive.Value);
                }

                if (criteria.CreatedAfter.HasValue)
                {
                    query = query.Where(u => u.CreatedDate >= criteria.CreatedAfter.Value);
                }

                if (criteria.CreatedBefore.HasValue)
                {
                    query = query.Where(u => u.CreatedDate <= criteria.CreatedBefore.Value);
                }

                // Apply pagination (prevent large result sets)
                var users = await query
                    .OrderBy(u => u.Username)
                    .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                    .Take(criteria.PageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedDate = u.CreatedDate,
                        LastModifiedDate = u.LastModifiedDate,
                        LastLoginDate = u.LastLoginDate,
                        IsActive = u.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Search completed. Found {Count} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with criteria");
                throw;
            }
        }

        /// <summary>
        /// Securely create a new user using parameterized query
        /// </summary>
        /// <param name="user">User entity to create</param>
        /// <returns>Created user ID</returns>
        public async Task<int> CreateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                // Check if user already exists
                var existingUser = await UserExistsAsync(user.Username, user.Email);
                if (existingUser)
                {
                    throw new InvalidOperationException("User with this username or email already exists");
                }

                user.CreatedDate = DateTime.UtcNow;
                user.LastModifiedDate = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user: {Username} with ID: {UserId}", user.Username, user.Id);
                return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user?.Username);
                throw;
            }
        }

        /// <summary>
        /// Securely update user information using parameterized query
        /// </summary>
        /// <param name="id">User ID to update</param>
        /// <param name="user">Updated user data</param>
        /// <returns>True if successful, false if user not found</returns>
        public async Task<bool> UpdateUserAsync(int id, User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                    return false;

                // Update only allowed fields to prevent mass assignment vulnerabilities
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.LastModifiedDate = DateTime.UtcNow;
                existingUser.IsActive = user.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Securely delete user using parameterized query
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>True if successful, false if user not found</returns>
        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Securely deactivate user (soft delete) using parameterized query
        /// </summary>
        /// <param name="id">User ID to deactivate</param>
        /// <returns>True if successful, false if user not found</returns>
        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                user.IsActive = false;
                user.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deactivated user: {UserId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Check if user exists by username or email using parameterized query
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <param name="email">Email to check</param>
        /// <returns>True if user exists</returns>
        public async Task<bool> UserExistsAsync(string username, string email)
        {
            try
            {
                var exists = await _context.Users
                    .AnyAsync(u => u.Username == username || u.Email == email);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Username}, {Email}", username, email);
                throw;
            }
        }

        /// <summary>
        /// Get total count of active users using parameterized query
        /// </summary>
        /// <returns>Total user count</returns>
        public async Task<int> GetUserCountAsync()
        {
            try
            {
                var count = await _context.Users
                    .Where(u => u.IsActive)
                    .CountAsync();

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user count");
                throw;
            }
        }

        /// <summary>
        /// Securely validate user credentials using parameterized query
        /// </summary>
        /// <param name="username">Username parameter</param>
        /// <param name="passwordHash">Hashed password parameter</param>
        /// <returns>UserDto if credentials are valid, null otherwise</returns>
        public async Task<UserDto?> ValidateUserCredentialsAsync(string username, string passwordHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passwordHash))
                    return null;

                var user = await _context.Users
                    .Where(u => u.Username == username && 
                               u.PasswordHash == passwordHash && 
                               u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        CreatedDate = u.CreatedDate,
                        LastModifiedDate = u.LastModifiedDate,
                        LastLoginDate = u.LastLoginDate,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    _logger.LogInformation("User credentials validated successfully: {Username}", username);
                }
                else
                {
                    _logger.LogWarning("Failed credential validation attempt for: {Username}", username);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user credentials for: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Securely update user password using parameterized query
        /// </summary>
        /// <param name="userId">User ID parameter</param>
        /// <param name="newPasswordHash">New password hash parameter</param>
        /// <param name="salt">Salt parameter</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash, string salt)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                user.PasswordHash = newPasswordHash;
                user.Salt = salt;
                user.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password updated for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user with password information for authentication
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Full user entity with password data</returns>
        public async Task<User?> GetUserWithPasswordByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && u.IsActive)
                    .FirstOrDefaultAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with password by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.LastLoginDate = DateTime.UtcNow;
                user.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated last login for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                throw;
            }
        }
    }
}