using SafeVault.Models;

namespace SafeVault.Repositories
{
    public interface IUserRepository
    {
        // Secure CRUD operations with parameterized queries
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<IEnumerable<UserDto>> SearchUsersAsync(UserSearchCriteria criteria);
        Task<int> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(int id, User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> UserExistsAsync(string username, string email);
        Task<int> GetUserCountAsync();
        
        // Secure authentication-related queries
        Task<UserDto?> ValidateUserCredentialsAsync(string username, string passwordHash);
        Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash, string salt);
        Task<User?> GetUserWithPasswordByIdAsync(int id);
        Task<bool> UpdateLastLoginAsync(int userId);
    }
}