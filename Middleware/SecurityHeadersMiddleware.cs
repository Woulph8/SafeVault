using Microsoft.AspNetCore.Http.Headers;

namespace SafeVault.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            var response = context.Response;

            // Prevent XSS attacks - Use indexer to avoid duplicate key exceptions
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["X-XSS-Protection"] = "1; mode=block";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content Security Policy to prevent XSS
            response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "font-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data:; " +
                "connect-src 'self'";

            // HSTS (HTTP Strict Transport Security) for HTTPS enforcement
            if (context.Request.IsHttps)
            {
                response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Remove server information
            response.Headers.Remove("Server");

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}