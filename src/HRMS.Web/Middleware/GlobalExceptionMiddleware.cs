using System.Net;
using System.Text.Json;
using FluentValidation;
using HRMS.Core.Exceptions;
using HRMS.Shared.Constants;

namespace HRMS.Web.Middleware
{
    /// <summary>
    /// Global exception-handling middleware that catches unhandled exceptions, logs them
    /// with the current correlation ID, and returns a structured JSON error response.
    /// Business exceptions (HTTP 400/404) are logged at Warning level; system/unexpected
    /// exceptions are logged at Error level.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Items.TryGetValue(
                HrmsConstants.Logging.CorrelationIdItemKey, out var id)
                ? id?.ToString() ?? string.Empty
                : string.Empty;

            var (statusCode, errorCode, userMessage) = ClassifyException(exception);

            if (statusCode >= (int)HttpStatusCode.InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled system exception [{ErrorCode}] on {Method} {Path} | CorrelationId: {CorrelationId}",
                    errorCode,
                    context.Request.Method,
                    context.Request.Path,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "Business exception [{ErrorCode}] on {Method} {Path} | CorrelationId: {CorrelationId} | {Message}",
                    errorCode,
                    context.Request.Method,
                    context.Request.Path,
                    correlationId,
                    exception.Message);
            }

            // If the response has already started we cannot change the status code.
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = BuildErrorResponse(exception, statusCode, errorCode, userMessage, correlationId);
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Maps a known exception type to an HTTP status code, error code, and user-facing message.
        /// All unrecognised exceptions fall back to 500 Internal Server Error.
        /// </summary>
        private (int statusCode, string errorCode, string userMessage) ClassifyException(Exception ex)
        {
            return ex switch
            {
                NotFoundException nfe =>
                    ((int)HttpStatusCode.NotFound, nfe.ErrorCode, nfe.Message),

                BusinessException be =>
                    ((int)HttpStatusCode.BadRequest, be.ErrorCode, be.Message),

                ValidationException ve =>
                    ((int)HttpStatusCode.BadRequest, "VALIDATION_FAILED",
                     string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),

                KeyNotFoundException =>
                    ((int)HttpStatusCode.NotFound, "NOT_FOUND", "The requested resource was not found."),

                UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Unauthorized, "UNAUTHORIZED", "You are not authorized to perform this action."),

                InvalidOperationException ioe =>
                    ((int)HttpStatusCode.BadRequest, "INVALID_OPERATION", ioe.Message),

                _ =>
                    ((int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR",
                     "An unexpected error occurred. Please try again later.")
            };
        }

        private object BuildErrorResponse(
            Exception exception,
            int statusCode,
            string errorCode,
            string userMessage,
            string correlationId)
        {
            if (_environment.IsDevelopment())
            {
                return new
                {
                    status = statusCode,
                    errorCode,
                    message = userMessage,
                    detail = exception.Message,
                    stackTrace = exception.StackTrace,
                    correlationId
                };
            }

            return new
            {
                status = statusCode,
                errorCode,
                message = userMessage,
                correlationId
            };
        }
    }

    /// <summary>
    /// Extension methods for registering <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Adds <see cref="GlobalExceptionMiddleware"/> to the pipeline.
        /// This should be registered before all other middleware so that
        /// it can catch exceptions from any layer.
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
