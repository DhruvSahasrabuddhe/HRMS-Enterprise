using HRMS.Shared.Constants;

namespace HRMS.Web.Middleware
{
    /// <summary>
    /// Middleware to add security headers to HTTP responses.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // X-Frame-Options: Prevents clickjacking attacks
            context.Response.Headers["X-Frame-Options"] = HrmsConstants.Security.XFrameOptionsValue;

            // X-Content-Type-Options: Prevents MIME sniffing
            context.Response.Headers["X-Content-Type-Options"] = HrmsConstants.Security.XContentTypeOptionsValue;

            // X-XSS-Protection: Enables XSS filter in older browsers
            context.Response.Headers["X-XSS-Protection"] = HrmsConstants.Security.XXssProtectionValue;

            // Content-Security-Policy: Helps prevent XSS attacks
            context.Response.Headers["Content-Security-Policy"] = HrmsConstants.Security.ContentSecurityPolicy;

            // Referrer-Policy: Controls referrer information
            context.Response.Headers["Referrer-Policy"] = HrmsConstants.Security.ReferrerPolicyValue;

            // Permissions-Policy: Restricts browser features
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for registering security headers middleware.
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
