using HRMS.Shared.Constants;

namespace HRMS.Web.Middleware
{
    /// <summary>
    /// Middleware that reads an incoming <c>X-Correlation-ID</c> header (or generates a new
    /// <see cref="Guid"/> when none is present), stores the value in
    /// <see cref="HttpContext.Items"/>, echoes it back on the response, and adds it to the
    /// ambient log scope so every log entry emitted during the request is enriched with it.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
        {
            var correlationId = ResolveCorrelationId(context);

            // Store on the context so other middleware and controllers can read it.
            context.Items[HrmsConstants.Logging.CorrelationIdItemKey] = correlationId;

            // Propagate to the caller via response header.
            context.Response.Headers[HrmsConstants.Logging.CorrelationIdHeader] = correlationId;

            // Enrich every log entry emitted during this request with the correlation ID.
            using (logger.BeginScope(new Dictionary<string, object>
            {
                [HrmsConstants.Logging.CorrelationIdProperty] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(
                    HrmsConstants.Logging.CorrelationIdHeader, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing.ToString();
            }

            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Extension methods for registering <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        /// <summary>
        /// Adds <see cref="CorrelationIdMiddleware"/> to the request pipeline.
        /// This should be registered early so that all subsequent middleware and
        /// log entries carry the correlation ID.
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
