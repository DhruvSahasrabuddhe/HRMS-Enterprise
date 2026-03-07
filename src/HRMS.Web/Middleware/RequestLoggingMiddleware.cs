using System.Diagnostics;
using HRMS.Shared.Constants;

namespace HRMS.Web.Middleware
{
    /// <summary>
    /// Middleware to log all HTTP requests with IP address, user agent, response time,
    /// and the current correlation ID (set by <see cref="CorrelationIdMiddleware"/>).
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            // Capture request information
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = request.Headers["User-Agent"].ToString();
            var method = request.Method;
            var path = request.Path;
            var user = context.User?.Identity?.Name ?? "Anonymous";
            var correlationId = context.Items.TryGetValue(
                HrmsConstants.Logging.CorrelationIdItemKey, out var id)
                ? id?.ToString() ?? string.Empty
                : string.Empty;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var duration = stopwatch.ElapsedMilliseconds;

                // Log the request details
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms | User: {User} | IP: {IpAddress} | UserAgent: {UserAgent} | CorrelationId: {CorrelationId}",
                    method, path, statusCode, duration, user, ipAddress, userAgent, correlationId);

                // Log warning for slow requests
                if (duration > HrmsConstants.Logging.SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow request detected: {Method} {Path} took {Duration}ms | User: {User} | CorrelationId: {CorrelationId}",
                        method, path, duration, user, correlationId);
                }

                // Log warning for 4xx and 5xx errors
                if (statusCode >= 400)
                {
                    _logger.LogWarning(
                        "Request error: {Method} {Path} returned {StatusCode} | User: {User} | IP: {IpAddress} | CorrelationId: {CorrelationId}",
                        method, path, statusCode, user, ipAddress, correlationId);
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for registering request logging middleware.
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
