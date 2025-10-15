using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using SafeVault.Models;

namespace SafeVault.Services
{
    public interface IInputSanitizationService
    {
        SanitizedUserInput SanitizeAndValidateUserInput(UserInputModel input);
        string SanitizeString(string input);
        bool ContainsMaliciousContent(string input);
        string RemoveHtmlTags(string input);
        string EscapeSpecialCharacters(string input);
    }

    public class InputSanitizationService : IInputSanitizationService
    {
        private readonly ILogger<InputSanitizationService> _logger;
        
        // Dangerous patterns that could indicate malicious input
        private readonly string[] _maliciousPatterns = {
            // SQL Injection patterns
            @"('|(\\|%27|%2527))",
            @"(-|%2D|%252D){2,}",
            @"(;|%3B|%253B)",
            @"\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT(\s+INTO)?|MERGE|SELECT|UPDATE|UNION(\s+ALL)?)\b",
            
            // XSS patterns
            @"<\s*script",
            @"<\s*/?\s*script",
            @"javascript\s*:",
            @"vbscript\s*:",
            @"on\w+\s*=",
            @"<\s*iframe",
            @"<\s*object",
            @"<\s*embed",
            @"<\s*link",
            @"<\s*meta",
            
            // Command injection patterns
            @"[;&|`]",
            @"\$\(",
            @"\.\.[\\/]",
            @"\bcmd\s",
            @"\bdir\b",
            @"\becho\b",
            @"\bcat\b",
            @"\bls\b"
        };

        private readonly Dictionary<string, string> _htmlEntities = new()
        {
            { "<", "&lt;" },
            { ">", "&gt;" },
            { "\"", "&quot;" },
            { "'", "&#x27;" },
            { "&", "&amp;" },
            { "/", "&#x2F;" }
        };

        public InputSanitizationService(ILogger<InputSanitizationService> logger)
        {
            _logger = logger;
        }

        public SanitizedUserInput SanitizeAndValidateUserInput(UserInputModel input)
        {
            var result = new SanitizedUserInput();
            
            try
            {
                // Sanitize username
                if (!string.IsNullOrWhiteSpace(input.Username))
                {
                    result.Username = SanitizeString(input.Username.Trim());
                    
                    // Additional username validation
                    if (!IsValidUsername(result.Username))
                    {
                        result.ValidationErrors.Add("Username contains invalid characters or patterns");
                    }
                }
                else
                {
                    result.ValidationErrors.Add("Username is required");
                }

                // Sanitize email
                if (!string.IsNullOrWhiteSpace(input.Email))
                {
                    result.Email = SanitizeString(input.Email.Trim().ToLowerInvariant());
                    
                    // Additional email validation
                    if (!IsValidEmail(result.Email))
                    {
                        result.ValidationErrors.Add("Email format is invalid or contains suspicious content");
                    }
                }
                else
                {
                    result.ValidationErrors.Add("Email is required");
                }

                // Check for malicious content
                if (ContainsMaliciousContent(result.Username) || ContainsMaliciousContent(result.Email))
                {
                    result.ValidationErrors.Add("Input contains potentially malicious content");
                    _logger.LogWarning("Malicious input detected: Username={Username}, Email={Email}", 
                        result.Username, result.Email);
                }

                result.IsValid = result.ValidationErrors.Count == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during input sanitization");
                result.ValidationErrors.Add("An error occurred during input validation");
                result.IsValid = false;
            }

            return result;
        }

        public string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove null bytes
            input = input.Replace("\0", "");
            
            // Remove control characters except common whitespace
            input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // HTML encode dangerous characters
            input = EscapeSpecialCharacters(input);
            
            // Remove HTML tags
            input = RemoveHtmlTags(input);
            
            // Normalize whitespace
            input = Regex.Replace(input, @"\s+", " ");
            
            return input.Trim();
        }

        public bool ContainsMaliciousContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (var pattern in _maliciousPatterns)
            {
                try
                {
                    if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    {
                        return true;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // If regex times out, consider it suspicious
                    return true;
                }
            }

            return false;
        }

        public string RemoveHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove HTML tags
            return Regex.Replace(input, @"<[^>]+>", "");
        }

        public string EscapeSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Order matters - escape & first to avoid double escaping
            var orderedEntities = new Dictionary<string, string>
            {
                { "&", "&amp;" },
                { "<", "&lt;" },
                { ">", "&gt;" },
                { "\"", "&quot;" },
                { "'", "&#x27;" },
                { "/", "&#x2F;" }
            };

            var result = input;
            foreach (var entity in orderedEntities)
            {
                result = result.Replace(entity.Key, entity.Value);
            }

            return result;
        }

        private bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
                return false;

            // Only allow alphanumeric characters, dots, hyphens, and underscores
            return Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$");
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Basic email format validation
                var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return emailRegex.IsMatch(email) && email.Length <= 100;
            }
            catch
            {
                return false;
            }
        }
    }
}