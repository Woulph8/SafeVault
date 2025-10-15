using NUnit.Framework;
using Microsoft.Extensions.Logging;
using SafeVault.Services;
using SafeVault.Models;

namespace SafeVault.Tests
{
    [TestFixture]
    public class TestInputValidation
    {
        private InputSanitizationService _sanitizationService = null!;
        private ILogger<InputSanitizationService> _mockLogger = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new TestLogger();
            _sanitizationService = new InputSanitizationService(_mockLogger);
        }

        // Simple test logger implementation
        private class TestLogger : ILogger<InputSanitizationService>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) 
            {
                // Simple test logger - just ignore the logs
            }
        }

        [Test]
        public void TestForSQLInjection()
        {
            // Arrange
            var maliciousInputs = new[]
            {
                "admin'; DROP TABLE Users; --",
                "' OR '1'='1",
                "1' UNION SELECT * FROM Users--",
                "'; DELETE FROM Users WHERE 1=1; --"
            };

            foreach (var maliciousInput in maliciousInputs)
            {
                var userInput = new UserInputModel
                {
                    Username = maliciousInput,
                    Email = "test@example.com"
                };

                // Act
                var result = _sanitizationService.SanitizeAndValidateUserInput(userInput);

                // Assert
                Assert.That(result.IsValid, Is.False, $"SQL injection attempt should be detected: {maliciousInput}");
                Assert.That(result.ValidationErrors, Is.Not.Empty, "Should have validation errors for SQL injection attempt");
            }
        }

        [Test]
        public void TestForXSS()
        {
            // Arrange
            var xssInputs = new[]
            {
                "<script>alert('XSS')</script>",
                "javascript:alert('XSS')",
                "<img src='x' onerror='alert(1)'>",
                "<iframe src='javascript:alert(1)'></iframe>",
                "<svg onload='alert(1)'></svg>"
            };

            foreach (var xssInput in xssInputs)
            {
                var userInput = new UserInputModel
                {
                    Username = xssInput,
                    Email = "test@example.com"
                };

                // Act
                var result = _sanitizationService.SanitizeAndValidateUserInput(userInput);

                // Assert
                Assert.That(result.IsValid, Is.False, $"XSS attempt should be detected: {xssInput}");
                Assert.That(result.ValidationErrors, Is.Not.Empty, "Should have validation errors for XSS attempt");
            }
        }

        [Test]
        public void TestValidInput()
        {
            // Arrange
            var validInput = new UserInputModel
            {
                Username = "johndoe123",
                Email = "john.doe@example.com"
            };

            // Act
            var result = _sanitizationService.SanitizeAndValidateUserInput(validInput);

            // Assert
            Assert.That(result.IsValid, Is.True, "Valid input should pass validation");
            Assert.That(result.ValidationErrors, Is.Empty, "Valid input should not have validation errors");
            Assert.That(result.Username, Is.EqualTo("johndoe123"), "Username should be preserved");
            Assert.That(result.Email, Is.EqualTo("john.doe@example.com"), "Email should be preserved");
        }

        [Test]
        public void TestHtmlTagRemoval()
        {
            // Arrange
            var inputWithHtml = "<b>username</b>";

            // Act
            var sanitized = _sanitizationService.RemoveHtmlTags(inputWithHtml);

            // Assert
            Assert.That(sanitized, Is.EqualTo("username"), "HTML tags should be removed");
        }

        [Test]
        public void TestSpecialCharacterEscaping()
        {
            // Arrange
            var inputWithSpecialChars = "<script>alert('test');</script>";

            // Act
            var escaped = _sanitizationService.EscapeSpecialCharacters(inputWithSpecialChars);

            // Assert - Check that the result contains properly escaped characters
            Assert.That(escaped, Does.Contain("&lt;"), "< should be escaped to &lt;");
            Assert.That(escaped, Does.Contain("&gt;"), "> should be escaped to &gt;");
            Assert.That(escaped, Does.Contain("&#x27;"), "' should be escaped to &#x27;");
            
            // The expected result should be: &lt;script&gt;alert(&#x27;test&#x27;);&lt;&#x2F;script&gt;
            var expected = "&lt;script&gt;alert(&#x27;test&#x27;);&lt;&#x2F;script&gt;";
            Assert.That(escaped, Is.EqualTo(expected), $"Escaped string should match expected format. Got: {escaped}");
        }

        [Test]
        public void TestMaliciousContentDetection()
        {
            // Arrange
            var maliciousInputs = new[]
            {
                "SELECT * FROM Users",
                "<script>",
                "javascript:",
                "'; DROP TABLE",
                "cmd /c dir"
            };

            foreach (var input in maliciousInputs)
            {
                // Act
                var isMalicious = _sanitizationService.ContainsMaliciousContent(input);

                // Assert
                Assert.That(isMalicious, Is.True, $"Should detect malicious content: {input}");
            }
        }

        [Test]
        public void TestEmptyAndNullInputs()
        {
            // Arrange & Act & Assert
            var emptyResult = _sanitizationService.SanitizeString("");
            var nullResult = _sanitizationService.SanitizeString(string.Empty);

            Assert.That(emptyResult, Is.EqualTo(""), "Empty string should return empty string");
            Assert.That(nullResult, Is.EqualTo(""), "Empty string should return empty string");

            // Test with UserInputModel
            var emptyUserInput = new UserInputModel { Username = "", Email = "" };
            var result = _sanitizationService.SanitizeAndValidateUserInput(emptyUserInput);
            
            Assert.That(result.IsValid, Is.False, "Empty inputs should not be valid");
            Assert.That(result.ValidationErrors.Count, Is.GreaterThan(0), "Should have validation errors for empty inputs");
        }
    }
}